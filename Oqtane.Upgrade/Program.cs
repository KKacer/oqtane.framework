﻿using System.IO;
using System.Reflection;
using System.Threading;

namespace Oqtane.Upgrade
{
    class Program
    {
        static void Main(string[] args)
        {
            string binfolder = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            // assumes that the application executable must be deployed to the /bin of the Oqtane.Server project
            if (binfolder.Contains("Oqtane.Server\\bin"))
            {
                // ie. Oqtane.Server\bin\Debug\netcoreapp3.0\Oqtane.Upgrade.exe
                string rootfolder = Directory.GetParent(binfolder).Parent.Parent.FullName;
                string deployfolder = Path.Combine(rootfolder, "wwwroot\\Framework");

                // take the app offline
                if (File.Exists(Path.Combine(rootfolder, "app_offline.bak")))
                {
                    File.Move(Path.Combine(rootfolder, "app_offline.bak"), Path.Combine(rootfolder, "app_offline.htm"));
                }

                if (Directory.Exists(deployfolder))
                {
                    string filename;
                    string[] files = Directory.GetFiles(deployfolder);
                    if (CanAccessFiles(files, binfolder))
                    {
                        // backup the files 
                        foreach (string file in files)
                        {
                            filename = Path.Combine(binfolder, Path.GetFileName(file));
                            if (File.Exists(filename))
                            {
                                File.Move(filename, filename.Replace(".dll", ".bak"));
                            }
                        }

                        // copy the new files
                        bool success = true;
                        try
                        {
                            foreach (string file in files)
                            {
                                filename = Path.Combine(binfolder, Path.GetFileName(file));
                                // delete the file from the /bin if it exists
                                if (File.Exists(filename))
                                {
                                    File.Delete(filename);
                                }
                                // copy the new file to the /bin
                                File.Move(Path.Combine(deployfolder, Path.GetFileName(file)), filename);
                            }
                        }
                        catch
                        {
                            // an error occurred deleting or moving a file
                            success = false;
                        }

                        // restore on failure
                        if (!success)
                        {
                            foreach (string file in files)
                            {
                                filename = Path.Combine(binfolder, Path.GetFileName(file));
                                if (File.Exists(filename))
                                {
                                    File.Move(filename, filename.Replace(".bak", ".dll"));
                                }
                            }
                        }
                    }
                }

                // bring the app back online
                if (File.Exists(Path.Combine(rootfolder, "app_offline.htm")))
                {
                    File.Move(Path.Combine(rootfolder, "app_offline.htm"), Path.Combine(rootfolder, "app_offline.bak"));
                }
            }
 
            return;
        }

        private static bool CanAccessFiles(string[] files, string folder)
        {
            // ensure files are not locked by another process - the shutdownTimeLimit defines the duration for app shutdown
            bool canaccess = true;
            FileStream stream = null;
            int i = 0;
            while (i < (files.Length - 1) && canaccess)
            {
                string filepath = Path.Combine(folder, Path.GetFileName(files[i]));
                int attempts = 0;
                bool locked = true;
                // try up to 30 times
                while (attempts < 30 && locked == true)
                {
                    try
                    {
                        stream = System.IO.File.Open(filepath, FileMode.Open, FileAccess.Read, FileShare.None);
                        locked = false;
                    }
                    catch // file is locked by another process
                    {
                        Thread.Sleep(1000); // wait 1 second
                    }
                    finally
                    {
                        if (stream != null)
                        {
                            stream.Close();
                        }
                    }
                    attempts += 1;
                }
                if (locked && canaccess)
                {
                    canaccess = false;
                }
                i += 1;
            }
            return canaccess;
        }
    }
}