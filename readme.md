A tool used to transfer media content from an iphone to a pc.

##The Problem 
Iphone continuously disconnects during the media transfering process.
Windows explorer would freeze and the apple iphone stops being recognized. 
This always happened after a few minutes of transfering some content. 
This program will transfer files from the iphone and download them to the pc despite this issue

##How it works
On first launch with the iphone ready and connected to the pc, the program will mark all media files that are going to be transfered.
After, the program will start the transfer process following this download list.
Downloading in batches, the program will attempt to download n files before sleeping for a given number of miliseconds. 
  
If a download fails, the path of the failed file is logged into a failed text file. 

After attempting to download all files atleast once, the program then attempts to download in batches from the text file holding the failures.
Successful files are then removed after verification (based on file size)
The program will loop and continue to download from this text file until it becomes empty and no more failed files are present.

Onscreen instructions will notify when to remove and reconnect your iphone during this process


###Before using this program, these steps may resolve your issue 
1. use a usb port on the back
2. restart computer
3. In the iphone ensure that use original file is checked (iphone -> photos -> use original file)
4. if iphone doesn't appear in "This PC":
		unplug iphone
		terminate explorer.exe and restart explorer.exe