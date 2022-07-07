using System;
using System.IO;
using MediaDevices;
using System.Collections;
using System.Threading;
namespace CopyAllFiles
{
    class Program
    {


        static void Main(string[] args)
        {
            string downloadPath = "D:\\Projects\\CopyAllFiles\\downloaded";
            string failedFilesTxtPath = "D:\\Projects\\CopyAllFiles\\failed.txt";
            string initDirectoryPath = "/";
            int filesPerBatch = 100;
            int batchCounter = 0;
            int totalFilesDownloadedCounter = 0;
            int timeToSleepPerBatchMS = 5000;
            //Create fail list, or zero it if it already exists
            try { File.WriteAllText(failedFilesTxtPath, ""); } catch (Exception e) { }
            MediaDevice iphone = null;
            foreach (MediaDevice device in MediaDevice.GetDevices())
            {
                if (device.Description.Equals("Apple iPhone")) { iphone = device; break; }
            }
            if (!iphone.IsConnected) { iphone.Connect(); }
            ArrayList allDirectories = new ArrayList();
            ArrayList allFiles = new ArrayList();

            //crawl through all directory names and add to list
            string[] initDirectories = iphone.GetDirectories(initDirectoryPath);
            //crawl definition
            void crawlDirectories(string[] directories)
            {
                foreach (string directory in directories)
                {
                    Console.WriteLine("Queing Directory: " + directory);
                    allDirectories.Add(directory);
                    crawlDirectories(iphone.GetDirectories(directory));
                }
            }
            crawlDirectories(initDirectories);
            //grab all files
            foreach (string directory in allDirectories)
            {
                string[] files = iphone.GetFiles(directory);
                foreach (string file in files)
                {
                    Console.WriteLine("Queing file: " + file);
                    allFiles.Add(file);
                }
            }
            //Download function, reattempt determines whether to log failures into text file
            void DownloadFiles(ArrayList filesToDownload, Boolean reattempt = false, ArrayList filesMarkedForDeletion = null)
            {

                //download files
                foreach (string file in filesToDownload)
                {
                    //check if existing files are same as original
                    try
                    {
                        FileInfo copiedFileSize = new FileInfo(downloadPath + file);
                        ulong originalFileSizeBytes = iphone.GetFileInfo(file).Length;
                        ulong copiedFileSizeBytes = (ulong)copiedFileSize.Length;
                        //notify user of existing file, if its a reattempt remove existing file from failed list
                        if (originalFileSizeBytes == copiedFileSizeBytes) { Console.WriteLine("File already exists: " + file); if (reattempt) { filesMarkedForDeletion.Add(file); } continue; }
                    }
                    catch (Exception b) { }
                    //sleep per batch
                    if (batchCounter >= filesPerBatch) { Console.WriteLine("Sleeping for a bit, current files downloaded: " + totalFilesDownloadedCounter); if (iphone.IsConnected) { iphone.Disconnect(); } Thread.Sleep(timeToSleepPerBatchMS); batchCounter = 0; }
                    batchCounter++;
                    foreach (MediaDevice device in MediaDevice.GetDevices())
                    {
                        if (device.Description.Equals("Apple iPhone")) { iphone = device; break; }
                    }
                    if (!iphone.IsConnected) { iphone.Connect(); }
                    try
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(downloadPath + file));
                        using (FileStream fileWriter = File.Create(downloadPath + file))
                        {
                            Console.WriteLine("Downloading file: " + downloadPath + file);
                            //iphone.DownloadFile(file, fileStream);
                            iphone.DownloadFile(file, fileWriter);
                            fileWriter.Seek(0, SeekOrigin.Begin);
                            fileWriter.Flush();


                            //compare files 
                            FileInfo copiedFileSize = new FileInfo(downloadPath + file);
                            ulong originalFileSizeBytes = iphone.GetFileInfo(file).Length;
                            ulong copiedFileSizeBytes = (ulong)copiedFileSize.Length;
                            if (originalFileSizeBytes != copiedFileSizeBytes) { Console.WriteLine("Mismatch in filesize for file " + file + "! Original: " + originalFileSizeBytes + " Copy: " + copiedFileSizeBytes); throw new Exception("Copied filesize doesn't match original"); }
                            Console.WriteLine("Successfully downloaded: " + downloadPath + file);
                            totalFilesDownloadedCounter++;
                            //remove from fail list if reattempt
                            if (reattempt) { filesMarkedForDeletion.Add(file); }
                        }
                    }
                    catch (System.NotImplementedException g)
                    {

                    }
                    catch (System.Runtime.InteropServices.COMException u)
                    {
                        if (!reattempt) { File.AppendAllText(failedFilesTxtPath, file + "\n"); }
                        Console.WriteLine("Encountered bad file: " + file + " Message: " + u.Message);

                    }
                    catch (Exception e)
                    {
                        if (!reattempt) { File.AppendAllText(failedFilesTxtPath, file + "\n"); }
                        Console.WriteLine("Error for file: " + file + " Message: " + e.Message);

                    }
                }
            }
            //Attempt to download all queued files
            DownloadFiles(allFiles);
            //Check fail log
            string[] failedFiles = File.ReadAllLines(failedFilesTxtPath);
            //keep track of failed files and reattempts
            ArrayList failedFilesToDownloadList = new ArrayList();
            foreach (string fileToDownload in failedFiles)
            {
                Console.WriteLine("Queing failed file: " + fileToDownload);
                failedFilesToDownloadList.Add(fileToDownload);
            }
            //parse empty lines from the list
            Console.WriteLine("Clearing empty paths from queue");
            for (int x = 0; x < failedFilesToDownloadList.Count; x++)
            {
                if (((string)failedFilesToDownloadList[x]).Replace("\n", "").Trim().Equals(string.Empty))
                {
                    failedFilesToDownloadList.RemoveAt(x);
                }
            }
            if (failedFilesToDownloadList.Count > 0) { Console.WriteLine("Failed to download " + failedFilesToDownloadList.Count + " files"); }
            Console.WriteLine("Attempting to redownload failed files");
            Boolean exitProgram = false;
            //while there are failed files and user hasn't exited program
            while (failedFilesToDownloadList.Count > 0 && !exitProgram)
            {
                ArrayList filesMarkedForDeletion = new ArrayList();
                Console.WriteLine("Files Remaining: " + failedFilesToDownloadList.Count);
                //disconnect iphone and pause to allow user to fix issue
                if (iphone.IsConnected) { iphone.Disconnect(); }
                Console.WriteLine("Please reconnect device and ensure files can be read");
                Console.WriteLine("Press y to continue or n to stop");
                ConsoleKeyInfo keyPress = Console.ReadKey();
                while (!(keyPress.Key == ConsoleKey.Y) && !(keyPress.Key == ConsoleKey.N)) { Console.WriteLine("\nIncorrect key detected: " + keyPress.Key); keyPress = Console.ReadKey(); }
                Console.WriteLine("\n");
                if (keyPress.Key == ConsoleKey.Y)
                {
                    //attempt redownload
                    DownloadFiles(failedFilesToDownloadList, true, filesMarkedForDeletion);
                }
                else if (keyPress.Key == ConsoleKey.N)
                {
                    //exit program
                    exitProgram = true;
                    continue;
                }
                //remove marked files
                foreach (string fileToDelete in filesMarkedForDeletion)
                {
                    failedFilesToDownloadList.Remove(fileToDelete);
                }
            }
            //update failed files text
            String[] updatedFailedFilesText = new String[failedFilesToDownloadList.Count];
            failedFilesToDownloadList.CopyTo(updatedFailedFilesText);
            File.WriteAllLines(failedFilesTxtPath, updatedFailedFilesText);
            if (failedFilesToDownloadList.Count == 0) { Console.WriteLine("No failed files found"); }
            Console.WriteLine("Finished extraction!");
            if (updatedFailedFilesText.Length > 0) { Console.WriteLine("Number of files that failed to extract: " + updatedFailedFilesText.Length); }
            if (iphone.IsConnected) { iphone.Disconnect(); }




        }
    }
}
