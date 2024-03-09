import torch
import numpy as np
import cv2
from flask import Flask, request, send_file, jsonify
import io
import time
import torchvision
from collections import Counter
import torchvision.transforms as transforms
from concurrent.futures import ThreadPoolExecutor
import threading


class ObjectDetection:
    def __init__(self, weights):
        self.device = 'cuda' if torch.cuda.is_available() else 'cpu'
        print("\n\nDevice Used:", self.device)
        self.model = self.load_model(weights)
        self.classes = self.model.names

    def load_model(self, weights):
        # Load the YOLOv5 model with custom weights
        print("Hi")
        model = torch.hub.load('yolov5', 'custom', path=weights, source='local')
        model = model.eval() #evolution mode
        return model.to(self.device)

    def xywh2xyxy(self,x):
        # Convert nx4 boxes from [x, y, w, h] to [x1, y1, x2, y2] where xy1=top-left, xy2=bottom-right
        y = x.clone() if isinstance(x, torch.Tensor) else np.copy(x)
        y[..., 0] = x[..., 0] - x[..., 2] / 2  # top left x
        y[..., 1] = x[..., 1] - x[..., 3] / 2  # top left y
        y[..., 2] = x[..., 0] + x[..., 2] / 2  # bottom right x
        y[..., 3] = x[..., 1] + x[..., 3] / 2  # bottom right y
        return y
    
    def box_iou(self,box1, box2, eps=1e-7):
        # https://github.com/pytorch/vision/blob/master/torchvision/ops/boxes.py
        """
        Return intersection-over-union (Jaccard index) of boxes.
        Both sets of boxes are expected to be in (x1, y1, x2, y2) format.
        Arguments:
            box1 (Tensor[N, 4])
            box2 (Tensor[M, 4])
        Returns:
            iou (Tensor[N, M]): the NxM matrix containing the pairwise
                IoU values for every element in boxes1 and boxes2
        """

        # inter(N,M) = (rb(N,M,2) - lt(N,M,2)).clamp(0).prod(2)
        (a1, a2), (b1, b2) = box1.unsqueeze(1).chunk(2, 2), box2.unsqueeze(0).chunk(2, 2)
        inter = (torch.min(a2, b2) - torch.max(a1, b1)).clamp(0).prod(2)

        # IoU = inter / (area1 + area2 - inter)
        return inter / ((a2 - a1).prod(2) + (b2 - b1).prod(2) - inter + eps)

    def non_max_suppression(self,
        prediction,
        conf_thres=0.35,
        iou_thres=0.45,
        classes=None,
        agnostic=False,
        multi_label=False,
        labels=(),
        max_det=300,
        nm=0
        ):

        """Non-Maximum Suppression (NMS) on inference results to reject overlapping detections

        Returns:
            list of detections, on (n,6) tensor per image [xyxy, conf, cls]
        """

        # Checks
        assert 0 <= conf_thres <= 1, f'Invalid Confidence threshold {conf_thres}, valid values are between 0.0 and 1.0'
        assert 0 <= iou_thres <= 1, f'Invalid IoU {iou_thres}, valid values are between 0.0 and 1.0'
        if isinstance(prediction, (list, tuple)):  # YOLOv5 model in validation model, output = (inference_out, loss_out)
            prediction = prediction[0]  # select only inference output

        device = prediction.device
        mps = 'mps' in device.type  # Apple MPS
        if mps:  # MPS not fully supported yet, convert tensors to CPU before NMS
            prediction = prediction.cpu()
        bs = prediction.shape[0]  # batch size
        nc = prediction.shape[2] - nm - 5  # number of classes
        xc = prediction[..., 4] > conf_thres  # candidates

        # Settings
        # min_wh = 2  # (pixels) minimum box width and height
        max_wh = 7680  # (pixels) maximum box width and height
        max_nms = 30000  # maximum number of boxes into torchvision.ops.nms()
        time_limit = 0.5 + 0.05 * bs  # seconds to quit after
        redundant = True  # require redundant detections
        multi_label &= nc > 1  # multiple labels per box (adds 0.5ms/img)
        merge = False  # use merge-NMS

        t = time.time()
        mi = 5 + nc  # mask start index
        output = [torch.zeros((0, 6 + nm), device=prediction.device)] * bs
        for xi, x in enumerate(prediction):  # image index, image inference
            # Apply constraints
            # x[((x[..., 2:4] < min_wh) | (x[..., 2:4] > max_wh)).any(1), 4] = 0  # width-height
            x = x[xc[xi]]  # confidence

            # Cat apriori labels if autolabelling
            if labels and len(labels[xi]):
                lb = labels[xi]
                v = torch.zeros((len(lb), nc + nm + 5), device=x.device)
                v[:, :4] = lb[:, 1:5]  # box
                v[:, 4] = 1.0  # conf
                v[range(len(lb)), lb[:, 0].long() + 5] = 1.0  # cls
                x = torch.cat((x, v), 0)

            # If none remain process next image
            if not x.shape[0]:
                continue

            # Compute conf
            x[:, 5:] *= x[:, 4:5]  # conf = obj_conf * cls_conf

            # Box/Mask
            box = self.xywh2xyxy(x[:, :4])  # center_x, center_y, width, height) to (x1, y1, x2, y2)
            mask = x[:, mi:]  # zero columns if no masks

            # Detections matrix nx6 (xyxy, conf, cls)
            if multi_label:
                i, j = (x[:, 5:mi] > conf_thres).nonzero(as_tuple=False).T
                x = torch.cat((box[i], x[i, 5 + j, None], j[:, None].float(), mask[i]), 1)
            else:  # best class only
                conf, j = x[:, 5:mi].max(1, keepdim=True)
                x = torch.cat((box, conf, j.float(), mask), 1)[conf.view(-1) > conf_thres]

            # Filter by class
            if classes is not None:
                x = x[(x[:, 5:6] == torch.tensor(classes, device=x.device)).any(1)]

            # Apply finite constraint
            # if not torch.isfinite(x).all():
            #     x = x[torch.isfinite(x).all(1)]

            # Check shape
            n = x.shape[0]  # number of boxes
            if not n:  # no boxes
                continue
            x = x[x[:, 4].argsort(descending=True)[:max_nms]]  # sort by confidence and remove excess boxes

            # Batched NMS
            c = x[:, 5:6] * (0 if agnostic else max_wh)  # classes
            boxes, scores = x[:, :4] + c, x[:, 4]  # boxes (offset by class), scores
            i = torchvision.ops.nms(boxes, scores, iou_thres)  # NMS
            i = i[:max_det]  # limit detections
            if merge and (1 < n < 3E3):  # Merge NMS (boxes merged using weighted mean)
                # update boxes as boxes(i,4) = weights(i,n) * boxes(n,4)
                iou = self.box_iou(boxes[i], boxes) > iou_thres  # iou matrix
                weights = iou * scores[None]  # box weights
                x[i, :4] = torch.mm(weights, x[:, :4]).float() / weights.sum(1, keepdim=True)  # merged boxes
                if redundant:
                    i = i[iou.sum(1) > 1]  # require redundancy

            output[xi] = x[i]
            if mps:
                output[xi] = output[xi].to(device)
            if (time.time() - t) > time_limit:
                print(f'WARNING ⚠️ NMS time limit {time_limit:.3f}s exceeded')
                break  # time limit exceeded

        return output

    
    def prepro_image_old(self, img):
        img = cv2.cvtColor(img, cv2.COLOR_BGR2RGB) 
        img = torch.from_numpy(img.transpose((2, 0, 1))).float()  # convert to tensor
        img /= 255.0 
        # Run inference
        img = img.unsqueeze(0)  # add batch dimension
        img = img.to(self.device)
        return img

    
    def prepro_image(self, image_list):
        # Preprocess a batch of images
        batch_images = torch.stack([torch.from_numpy(img.transpose((2, 0, 1))).float() / 255.0 for img in image_list])
        return batch_images.to(self.device)


    def put_labels(self, img, class_ids):
        # Put labels and result on the image
        font = cv2.FONT_HERSHEY_SIMPLEX
        font_scale = 1
        font_color = (238, 245, 6)
        line_thickness = 2
        line_type = cv2.LINE_AA

        class_ids = set(class_ids)  

        if 2 in class_ids:
            if 0 in class_ids:
                text = "Chamfer Missing"
                result = "Defect Not Ok"
            else:
                text = "Chamfer Missing"
                result = "Ok"
        elif 0 in class_ids and 1 in class_ids:
            text = "Chamfer Present"
            result = "Defect Not Ok"
        elif 0 in class_ids:
            text = "Defect Chamfer Present"
            result = "Not Ok"
        else:
            text = "Chamfer Present"
            result = "Ok"

        (text_width, text_height), _ = cv2.getTextSize(text, font, font_scale, line_thickness)

        text = "Chamfer Present"
        position_x = (img.shape[1] - text_width) // 2
        position_y = text_height + 10
        position = (position_x, position_y + 80)
        cv2.putText(img, text, position, font, font_scale, font_color, line_thickness, line_type)

        font_color = (255, 0, 255)
        position = (img.shape[1] - text_width - 10, 10 + text_height + 80)
        cv2.putText(img, result, position, font, font_scale, font_color, line_thickness, line_type)

        return img

    def score_frame(self, frame_list):
        # Score the frames by performing object detection and applying non-maximum suppression
        batch_images = self.prepro_image(frame_list)
        with torch.no_grad():
            results = self.model(batch_images)

        det = self.non_max_suppression(results, conf_thres=0.35, iou_thres=0.45)

        label_color = {"Defect": (0, 0, 255), "Chamfer": (0, 255, 0), "Edge": (0, 255, 0)}
        pro_frame = []

        for i, (frame, dett) in enumerate(zip(frame_list, det)):
            class_ids = []
            for *xyxy, conf, cls in dett:
                lab = self.model.names[int(cls)]
                label = f'{lab} {conf:.2f}'

                if lab == "Edge":
                    label = "Chamfer"
                bgr = label_color.get(lab, (0, 0, 0))
                class_ids.append(int(cls))

                cv2.rectangle(frame, (int(xyxy[0]), int(xyxy[1])), (int(xyxy[2]), int(xyxy[3])), bgr, 2)
                cv2.putText(frame, label, (int(xyxy[0]), int(xyxy[1]) - 10),
                            cv2.FONT_HERSHEY_SIMPLEX, 0.8, bgr, 2, cv2.LINE_AA)

            frame = self.put_labels(frame, class_ids)
            pro_frame.append(frame)

        return pro_frame

    
pts1 = np.float32([[0, 480], [1180, 0], [0, 1800], [1200, 1550]])
pts2 = np.float32([[0, 0], [1900, 0], [0, 1220], [1900, 1220]])
M = cv2.getPerspectiveTransform(pts1, pts2)
def pers_transf(img):
    # Perform perspective transformation on the image
    dst = cv2.warpPerspective(img, M, (1900, 1220))
    return dst

# Load the model
detection = ObjectDetection(weights=r'F:/yolov5_TeethDent/yolov5/best.pt')

### Define Flask api
##app = Flask(__name__)
##
##@app.route("/")
##def home():
##    return {"Health": "OK"}
##
##@app.route('/predict_cover', methods=['POST'])
def predictions(imgPath):
    start_time = time.time()
##    file = request.files['image']
##    image_bytes = file.read()
##    frame = cv2.imdecode(np.frombuffer(image_bytes, np.uint8), cv2.IMREAD_COLOR) #decode_image
    #frame=pers_transf(frame) #correction prespective
    #frame = cv2.resize(frame, (640,640))
    frame_list=[]
    ls=["F://yolov5_TeethDent//data//2_transformed.jpg","F://yolov5_TeethDent//data//17_transformed.jpg"]
    end_time7 = time.time()
    execution_time_ms7 = (end_time7 - start_time) * 1000
    print("Execution time for reciving img :", execution_time_ms7, "ms")
    
    # Load the images from file paths in parallel using multi-threading
    with ThreadPoolExecutor() as executor:
        def load_image(path):
            img = cv2.imread(path)
            img = cv2.resize(img, (640,640))
            frame_list.append(img)

        # Submit the image loading tasks to the executor
        executor.map(load_image, ls)

    start_time6 = time.time()
    Img_list = detection.score_frame(frame_list)
    end_time6 = time.time()
    execution_time_m6s = (end_time6 - start_time6) * 1000
    print("Execution time For Detection res :", execution_time_m6s, "ms")

    img1=Img_list[0]
    img2=Img_list[1]
    #img_combined = cv2.hconcat([img1, img2])
    img_combined = np.concatenate((img1, img2), axis=1)

    end_time = time.time()
    execution_time_ms = (end_time - start_time) * 1000
    print("Execution time:", execution_time_ms, "ms")

    #return jsonify(Img_list)

    #return send_file(io.BytesIO(cv2.imencode('.jpg', img_combined)[1]), mimetype='image/jpeg')#send_file(img_io, mimetype='image/jpeg')

##
##if __name__ == '__main__':
##    app.run()

import glob
temp=0

imgPath = r"F:\yolov5_TeethDent\data\*.jpg" #r"F:\data_all\*.png" #
for file in glob.glob(imgPath):
    #parameter_list=[]
    
    #parameter_list=load_network()
    imgPath = file
    #print(file)
    
##    cv2.imshow("img", img)
##    cv2.waitKey(0)

    predictions(imgPath)
##    if cv2.waitKey(0) & 0xFF == ord('q'):
##        break
##    temp+=1
##    print("Image count : ",temp)
##    cv2.waitKey(0)
##    cv2.destroyAllWindows()
