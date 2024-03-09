import cv2
import matplotlib.pyplot as plt
import numpy as np
import warnings
warnings.filterwarnings('ignore')
img = cv2.imread(r'C:\Users\sbpl.tsline\Desktop\2.bmp')
print("shape of image : ",img.shape)
#rows,cols = img.shape
#def pers_transf(img):
  
##pts1 = np.float32([[0,150],[1180,0],[0,1800],[1200,1550]])
##  #top_left(x,y),top_right,bottom_left,bottom_right
##pts2 = np.float32([[0,0],[1900,0],[0,1220],[1900,1220]])
##M = cv2.getPerspectiveTransform(pts1,pts2)
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
dst=pers_transf(img)
img=cv2.cvtColor(img,cv2.COLOR_BGR2RGB)
dst=cv2.cvtColor(dst,cv2.COLOR_BGR2RGB)
plt.subplot(121),plt.imshow(img),plt.title('Input')
plt.subplot(122),plt.imshow(dst),plt.title('Output')
plt.show()

