import torch
import numpy as np
import cv2
from flask import Flask, jsonify,request, send_file
import json, os, signal
import base64
from PIL import Image
import io
import time
import torchvision
from collections import Counter


class DefectData:
    def __init__(self, defType, coordinates):
        self.defType = defType
        self.coordinates = coordinates


class ObjectDetection:
    def __init__(self, weights):
        self.model = self.load_model(weights)
        self.classes = self.model.names
        self.device = 'cuda' if torch.cuda.is_available() else 'cpu'
    
        print("\n\nDevice Used:", self.device)

    def load_model(self, weights):
        model = torch.hub.load('yolov5', 'custom', path=weights,source='local')
        return model

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
        conf_thres=0.5,
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

    
    def prepro_image(self, img):
        img = cv2.cvtColor(img, cv2.COLOR_BGR2RGB) 
        img = torch.from_numpy(img.transpose((2, 0, 1))).float()  # convert to tensor
        img /= 255.0 
        # Run inference
        img = img.unsqueeze(0)  # add batch dimension
        img = img.to(self.device)
        return img
    
    def put_labels(self,img,class_ids):
        font = cv2.FONT_HERSHEY_SIMPLEX
        font_scale = 1
        font_color = (238, 245, 6)
        line_thickness = 2
        line_type = cv2.LINE_AA

        class_ids = set(class_ids)  # Assuming class_ids is defined elsewhere
        print("class_ids",class_ids)
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

    def score_frame(self, frame): 
        self.model.to(self.device)
        pro_img = self.prepro_image(frame)
        #frame = [frame]
        results = self.model(pro_img)
        
        det = self.non_max_suppression(results, conf_thres=0.45, iou_thres=0.45)[0]

        # Move the image tensor to CPU and convert to a NumPy array
        #img = frame.squeeze().permute(1, 2, 0).cpu().numpy()

        # Draw bounding boxes and labels
        #img = cv2.cvtColor(img, cv2.COLOR_RGB2BGR)  # convert tensor back to BGR image
        label_color = {
            "SurfaceDefect": (0, 0, 255),
            "ChamferPresent": (0, 255, 0),
            "ChamferMiss": (0, 0, 255)
            }
        class_ids=[]
        cords=[]
        lab_list=[]

        for *xyxy, conf, cls in det:
            lab=self.model.names[int(cls)]
            
            if lab=="ChamferMiss" and int(xyxy[1])<=320:
                label="Chamfer miss upper"
                lab_list.append(label)
            elif lab=="ChamferMiss" and int(xyxy[1])>=320:
                label="Chamfer miss lower"
                lab_list.append(label)
            elif lab== "ChamferPresent":
                lab_list.append(lab)
                pass
            else:
                label = f'{lab} {conf:.2f}'
                lab_list.append(label)
            bgr = label_color.get(lab, (0, 0, 0))
            class_ids.append(int(cls))
            li=[lab,[int(xyxy[0]),int(xyxy[1]),int(xyxy[2]), int(xyxy[3])]]
            print("cords for single obj :",li)
            #defect = DefectData(lab, li[1])
            #defectDataList.append(defect)
            print("Defect added to list")
            cords.append(li)
            #
##            if lab== "ChamferPresent":
##                pass
##            else:
            frame = cv2.rectangle(frame, (int(xyxy[0]), int(xyxy[1])), (int(xyxy[2]), int(xyxy[3])), bgr, 2)
            frame = cv2.putText(frame, label, (int(xyxy[0]), int(xyxy[1]) - 10), cv2.FONT_HERSHEY_SIMPLEX, 0.8, bgr, 2,cv2.LINE_AA)
            ##frame = cv2.rectangle(frame, (int(xyxy[0]), int(xyxy[1])), (int(xyxy[2]), int(xyxy[3])), bgr, 2)
            #frame = cv2.putText(frame, label, (int(xyxy[0]), int(xyxy[1]) - 10), cv2.FONT_HERSHEY_SIMPLEX, 0.8, bgr, 2,cv2.LINE_AA)
            
        #frame=self.put_labels(frame,class_ids)
        print("all cords : ",cords)
        if "SurfaceDefect" in lab_list or "Chamfer miss upper" in lab_list or "Chamfer miss upper" in lab_list:
            lab_list.insert(0,"Ng")
        else:
            lab_list.insert(0,"Ok")
##        adjust_boxes=self.adjust_boxes_to_original_image(cords)
##        print("adjust_boxes : ",adjust_boxes)
##        defectDataList = []
##        for box in adjust_boxes:
##            defect = DefectData(box[0], box[1])
##            defectDataList.append(defect)
        print("lab_list :",lab_list)
        lab_list=list(set(lab_list))
        return lab_list,frame

##dst = cv2.warpPerspective(img,M,(1900,1220))
def pers_transf(img):
  
  #pts1 = np.float32([[0,480],[1180,0],[0,1800],[1200,1550]])
  pts1 = np.float32([[0, 445], [1175, 135], [0, 1680], [1150, 1280]])
  #top_left(x,y),top_right,bottom_left,bottom_right
  pts2 = np.float32([[0,0],[950,0],[0,610],[950,610]])
  M = cv2.getPerspectiveTransform(pts1,pts2)
  dst = cv2.warpPerspective(img,M,(950,610))
  dst = dst[:,100:850]
  return dst
# Load the model
detection = ObjectDetection(weights=r'./yolov5/best.pt')

def serialize_defect(defect):
    return {
        "defType": defect.defType,
        "coordinates": defect.coordinates
    }

def stringToImage(base64_string):
    imgdata = base64.b64decode(base64_string)
    return Image.open(io.BytesIO(imgdata))

def opencv_image_to_base64(image):
    # Encode the OpenCV image as a JPEG image in memory
    _, buffer = cv2.imencode('.jpg', image)
    
    # Convert the image buffer to a base64 string
    base64_string = base64.b64encode(buffer).decode('utf-8')
    
    return base64_string

app2 = Flask(__name__)

# Define Flask routes

def shutdown_server():
    os.kill(os.getpid(), signal.SIGINT)
    return jsonify({"success": True, "message": "Server is shutting down..."})
    
@app2.route('/shutdown')
def shutdown():
    shutdown_server()
    return 'Server shutting down...'

@app2.route("/ServerCheck")
def ServerCheck():
    print("In ServerCheck")
    return {"Server": "OK"}

@app2.route("/")
def home():
    return {"Health": "OK"}

@app2.route('/predict_cover', methods=['POST'])
def predictions():
    try:
        print("In predict_cover")
        file = json.loads(request.data)# request.files['image']
        #print(file)
        image_bytes = file
        imgFromcs = stringToImage(image_bytes)
        numpyImage = np.array(imgFromcs)
        print("Image converted to numpy successfuly orinf server")
##        file = request.files['image']
##        image_bytes = file.read()
##        frames = cv2.imdecode(np.frombuffer(image_bytes, np.uint8), cv2.IMREAD_COLOR)
        start=time.time()
        openCvImage = cv2.cvtColor(numpyImage, cv2.COLOR_RGB2BGR)
        #frame = cv2.imdecode(np.frombuffer(image_bytes, np.uint8), cv2.IMREAD_COLOR)
        #cv2.imshow("Recieved Image", openCvImage)
        #cv2.waitKey(0)
        #cv2.imwrite("before prespective1.jpg", openCvImage)
        frame=pers_transf(openCvImage) #correction prespective 
        #cv2.imwrite("after prespective1.jpg", frame)
        frame = cv2.resize(frame, (640,640))
        #cv2.imwrite("org_check.jpg",frame)
        defList,det_frame = detection.score_frame(frame)
        
        print("defList : ",defList)
        #cv2.imshow("Resulted Image", det_frame)
        #cv2.waitKey(0)
        #cv2.imwrite("frame_with_boxes.jpg", frame_with_boxes)

       # _, img_encoded = cv2.imencode('.jpg', frame_with_boxes)  
        base64Image = opencv_image_to_base64(det_frame)
        #img_io = io.BytesIO(img_encoded.tobytes())
        print("detections with lablel :",defList)
        #serialized_defects = [serialize_defect(defect) for defect in defList]
        #print("hgyghmnhbik : ",jsonify(defetctData = defList))
        return jsonify(defImage=base64Image, serialized_Defects=defList)
        #return send_file(img_io, mimetype='image/jpeg')
    except Exception as ex:
        print("Exception in predict_cover ", ex)
        return ex



if __name__ == '__main__':
    #app.run()
    app2.run(port=5001)


