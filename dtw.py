import csv
import numpy
from scipy.spatial.distance import euclidean,cosine,hamming,braycurtis

from fastdtw import fastdtw

reader=csv.reader(open("bookA203849PM.csv_3.csv","rb"),delimiter=',')
x=list(reader)
csv1=numpy.array(x).astype('float')

reader=csv.reader(open("motherP205359PM.csv_1.csv","rb"),delimiter=',')
x=list(reader)
csv2=numpy.array(x).astype('float')

reader=csv.reader(open("bookP202302PM.csv_1.csv","rb"),delimiter=',')
x=list(reader)
csv3=numpy.array(x).astype('float')


distance, path = fastdtw(csv1, csv2, dist=cosine)


print(distance)

distance, path = fastdtw(csv2, csv3, dist=cosine)


print(distance)

distance, path = fastdtw(csv1, csv3, dist=cosine)


print(distance)


