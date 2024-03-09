##import torch
##
##if torch.cuda.is_available():
##    print("cuda")
##    print("version : ",torch.version.cuda)
##else:
##    print("cpu")
    
import tensorflow as tf
tf.config.list_physical_devices('GPU')
