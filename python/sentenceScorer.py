import csv
import numpy
import scipy
from scipy.spatial.distance import euclidean,cosine,hamming,braycurtis
from fastdtw import fastdtw
import os
import re
from operator import itemgetter

path = 'C:\\Users\\arames25\\Downloads\\TestData\\splitDataClean\\'

inputfile = 'C:\\Users\\arames25\\Downloads\\TestData\\splitDataClean\\'

def choose_iter(elements, length):
    for i in range(len(elements)):
        if length == 1:
            yield (elements[i],)
        else:
            for next in choose_iter(elements[i+1:len(elements)], length-1):
                yield (elements[i],) + next
def choose(l, k):
    return list(choose_iter(l, k))
supermasterStringList = []
for fichier in os.listdir(path):
    print("TEST_DATA")
    print(re.split("[0-9]", fichier)[0])
    findme = re.split("[0-9]", fichier)[0]
    reader = csv.reader(open(path + fichier, "r"), delimiter=',')
    y = list(reader)
    csv1 = numpy.array(y).astype('float')
    totest = os.listdir(path)
    totest.remove(fichier)
    distance = []
    for testfile in totest:
        #print("TEST_WITH_FILE")
        #print(re.split("[A-Z]", testfile)[0])
        classwithinitial = re.split("[0-9]", testfile)[0]
        classofdata = re.split("[A-Z]", testfile)[0]
        reader = csv.reader(open(path + testfile, "r"), delimiter=',')
        x = list(reader)
        csv2 = numpy.array(x).astype('float')
        a, b = fastdtw(csv1, csv2, dist=cosine)
        #if (classwithinitial[-1] != findme[-1]):
        distance.append((a,classwithinitial))
    distance.sort(key = lambda x: x[0])
    count = 0
    for aTuple in distance:
        count += 1
        print(aTuple)
        supermasterStringList.append(aTuple)
        if count == 10:
            break

(list(choose_iter(supermasterStringList,3)))
