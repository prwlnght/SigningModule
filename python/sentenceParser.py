
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

path = '/home/arun/Desktop/NLP/sentenceWordsData/'

data_path = '/home/arun/Desktop/NLP/allData/'
final_sentences = []
#inputfile = 'C:\\Users\\ppaudyal\\Google Drive\\School\\Fall2016\\NLP\\Project\\Data\\splitDataClean\\'
filename = ""
lengthofsentence = 0
for fichier in sorted(os.listdir(path)):
  order = re.split("_", fichier)[1]
  filename = filename + re.split("[A-Z]", order)[0] + "_"
  lengthofsentence = lengthofsentence + 1
text_file = open(filename+".txt", "w")

arraywords = []
arraywordprob = []

#for each file in files in this dir
for fichier in sorted(os.listdir(path)):
    print("TEST_DATA")
    order = re.split("_", fichier)[1]
    print(re.split("[0-9]", order)[0])
    findme = re.split("[A-Z]", order)[0]
    reader = csv.reader(open(path + fichier, "r"), delimiter=',')
    y = list(reader)
    csv1 = numpy.array(y).astype('float')
    totest = os.listdir(data_path)
    #totest.remove(fichier)
    distance = []
    arraywords = []
    text_file.write("#####"+findme+"#####")
    for testfile in totest:
        #print("TEST_WITH_FILE")
        #print(re.split("[A-Z]", testfile)[0])
        classwithinitial = re.split("[0-9]", testfile)[0]
        classofdata = re.split("[A-Z]", testfile)[0]
        reader = csv.reader(open(data_path + testfile, "r"), delimiter=',')
        x = list(reader)
        csv2 = numpy.array(x).astype('float')
        a, b = fastdtw(csv1, csv2, dist=correlation)
        #if (classwithinitial[-1] != findme[-1]):
        distance.append((a,classwithinitial))
	arraywords.append((classofdata,a))
	#arraywordprob.append(str(a)+"\n")
    distance.sort(key = lambda x: x[0])
    arraywords.sort(key = lambda x: x[1])
    
    #count = 0

    coolarray = []
    #for i in range(0,5):
	#count += 1
	
	
	#text_file.write(arraywords[i][0]+"\t"+str(arraywords[i][1])+"\n")
	#if count == 5:
            #break
    temparray = []
    count = 0
    for aTuple in arraywords:

	if count == 0:
  	  temparray.append(aTuple[0])
	  coolarray.append(aTuple)
	  count = count + 1
	  print(aTuple)
	elif aTuple[0] not in temparray:
	  temparray.append(aTuple[0])
	  coolarray.append(aTuple)
          print(aTuple)
	  count += 1
          if count == 5:
            arraywordprob.append(coolarray)
            break
    	else:
	  continue


# Python program recursively print all sentences that can be
# formed from list of word lists
R = lengthofsentence
C = lengthofsentence

# A recursive function to print all possible sentences that can
# be formed from a list of word list
def printUtil(arr,prob,m, n, output,out_prob):
 
    # Add current word to output array
    output[m] = arr[m][n]
    out_prob = out_prob*prob[m][n]
 #ADDD ANOTHER ARRAY CONTAINING VALUES
# ITERATE BOTH
    # If this is last word of current output sentence, then print
    # the output sentence
    if m==R-1:
        full_string = ""
        for i in xrange(R):
	    full_string = full_string + " "+ output[i]
            #print output[i] + " ",
	print full_string
        print str(out_prob)+"\n",
	final_sentences.append((full_string,out_prob))
	final_sentences.sort(key = lambda x: x[1])
	#print "###############",final_sentences
	#print "SORTED\n"
	print len(final_sentences),"#################"
	
        return
 
    # Recur for next row
    
    for i in xrange(C):
        if arr[m+1][i] != "":
            printUtil(arr,prob,m+1, i, output,out_prob)
 
# A wrapper over printUtil
def printf(arr,prob):
 
    # Create an array to store sentence
    output_final = [""] * lengthofsentence
    out_prob = 1 
    # Consider all words for first row as starting
    #  points and print all sentences
    for i in xrange(C):
        if arr[0][i] != "":
    		printUtil(arr,prob, 0, i, output_final,out_prob)
		for k,j in final_sentences:
	 	 #print k[0]+" "+str(k[1])+"\n"
	 	 text_file.write("\n"+str(1/j)+"\t"+k)
 
# Driver program
arr = [ ["you", "we",""],
        ["have", "are",""],
        ["sleep", "eat", "drink"]]
poss_wordset = []
poss_probset = []
for wordset in arraywordprob:
  poss_words = []
  poss_prob = []
  for word,prob in wordset:
    poss_words.append(word)
    poss_prob.append(prob)
  #print "word,prob"
  #print "word",len(poss_words)
  #print "prob",len(poss_prob)
  poss_wordset.append(poss_words)
  poss_probset.append(poss_prob) 
#print "wordset",(poss_wordset)
#print "probset",(poss_probset)
printf(poss_wordset,poss_probset)
#print(len(arraywordprob))
# This code is contributed by Bhavya Jain




# get clas of file from name, example from bookA.... get "book"
    #print(fichier)


