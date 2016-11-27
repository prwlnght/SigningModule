
#for each file in files

#get the class from the name

import csv
import numpy
import scipy
from scipy.spatial.distance import euclidean,cosine,hamming,braycurtis,correlation
from fastdtw import fastdtw
import os
import re
from operator import itemgetter

path = 'C:\\Users\\ppaudyal\\Google Drive\\School\\Fall2016\\NLP\\Project\\Data\\splitDataClean_old\\'

inputfile = 'C:\\Users\\ppaudyal\\Google Drive\\School\\Fall2016\\NLP\\Project\\Data\\splitDataClean\\'

testinputfile = 'C:\\Users\\ppaudyal\\Google Drive\\School\\Fall2016\\NLP\\Project\\Data\\splitDataCleanTest\\'



#for each file in files in this dir
for fichier in os.listdir(testinputfile):
    print("TEST_DATA")
    print(re.split("[0-9]", fichier)[0])
    findme = re.split("[0-9]", fichier)[0]
    reader = csv.reader(open(testinputfile + fichier, "r"), delimiter=',')
    y = list(reader)
    csv1 = numpy.array(y).astype('float')
    totest = os.listdir(path)
    #totest.remove(fichier)
    distance = []
    for testfile in totest:
        #print("TEST_WITH_FILE")
        #print(re.split("[A-Z]", testfile)[0])
        classwithinitial = re.split("[0-9]", testfile)[0]
        classofdata = re.split("[A-Z]", testfile)[0]
        reader = csv.reader(open(path + testfile, "r"), delimiter=',')
        x = list(reader)
        csv2 = numpy.array(x).astype('float')
        #print(csv1[:,7])
        #break
        a, b = fastdtw(csv1[:,6:12], csv2[:,6:12], dist=cosine)
        #if (classwithinitial[-1] != findme[-1]):
        distance.append((a,classwithinitial))
    distance.sort(key = lambda x: x[0])
    count = 0
    temparray = []
    for aTuple in distance:
        if count ==0:
          temparray.append(aTuple[1])
          count += 1
          print(aTuple)
        elif aTuple[1] not in temparray:
            temparray.append(aTuple[1])
            count += 1
            print(aTuple)
        if count == 30:
            break




# get clas of file from name, example from bookA.... get "book"
    #print(fichier)


