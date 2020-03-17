using Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;


namespace ProschlafUtils
{
    /// <summary>
    /// Used to keep track of the versions of WPF applications that are present on the current machine.
    /// </summary>
    public static class VersionControl
    {
        #region Vars
        const string FOLDER_PATH_OLD = @"Das Gesundheitshaus GmbH\LS 2.0"; //do NOT change this
        const string FOLDER_PATH_NEW = @"Das_Gesundheitshaus_GmbH\LS 2.0"; //this is a subfolder of the folder where the application config is stored (do NOT change this)
        #endregion

        /// <summary>
        /// Generates a file in the user's C:\ProgramData directory that contains the path to the current version of this software.
        /// </summary>
        /// <returns></returns>
        public static Exception UpdatePathToThisVersion(string applicationVersion, string pathToBaseDirectory)
        {
            if (string.IsNullOrEmpty(applicationVersion) || string.IsNullOrEmpty(pathToBaseDirectory))
                return new ArgumentException("Arguments are NULL or empty", "applicationVersion,pathToBaseDirectory");

            try
            {
                string directoryPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), FOLDER_PATH_NEW); //before version 1.0.19.19, "SpecialFolder.CommonApplicationData" was used (which sometimes caused UnauthorizedAccessExceptions)

                if (!Directory.Exists(directoryPath))
                {
                    var info = Directory.CreateDirectory(directoryPath);

                    if (info == null)
                        return new DirectoryNotFoundException("Path to program data directory not found: " + directoryPath);
                }

                string filePath = Path.Combine(directoryPath, applicationVersion + ".txt");

                //always replace the file
                if (File.Exists(filePath))
                {
                    File.SetAttributes(filePath, FileAttributes.Normal); //make sure the destination file is not protected in any way if it is existing
                    File.Delete(filePath);
                }

                File.WriteAllText(filePath, pathToBaseDirectory, Encoding.Unicode); //always write the current path

                //legacy: copy old files (can be removed when no more applications with version <= "1.0.19.18" are in use
                string oldDirectoryPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), FOLDER_PATH_OLD);
                if (Directory.Exists(oldDirectoryPath))
                {
                    try
                    {
                        //copy any existing version files to the new folder under 'C:\Users\<User>\AppData\Local'
                        string[] files = Directory.GetFiles(oldDirectoryPath);

                        if (files?.Length > 0)
                        {
                            foreach (var f in files)
                            {
                                string filePathNew = Path.Combine(directoryPath, Path.GetFileName(f));
                                if (!File.Exists(filePathNew))
                                    File.Copy(f, filePathNew);
                            }
                        }

                        Directory.Delete(oldDirectoryPath, true);
                    }
                    catch (Exception) { }
                }

                return null;
            }
            catch (Exception ex)
            {
                Logger.AddLogEntry(Logger.LogEntryCategories.Error, "Exception while trying to update path to current version", ex, "VersionControl");
                return ex;
            }
        }

        /// <summary>
        /// Gets the path to the newest version available.
        /// </summary>
        /// <returns>The info of the newest application or null.</returns>
        public static ApplicationPathInfo GetInfoForNewestVersion()
        {
            try
            {
                string directoryPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), FOLDER_PATH_NEW);

                if (!Directory.Exists(directoryPath))
                    return null;

                string[] files = Directory.GetFiles(directoryPath);

                if (files == null || files.Length == 0)
                    return null;

                var fileInfos = files.Select(f => new FileInfo(f));
                ApplicationPathInfo info = new ApplicationPathInfo();
                info.ApplicationVersion = new Version("0.0.0.1");

                foreach (FileInfo fileInfo in fileInfos.OrderBy(f => f.LastWriteTime)) //sort order is important here to make sure to get the latest file at last (which might already be the one we need) --> always use LastWriteTime because the original CreationDate gets lost when a file is copied
                {
                    string version = Path.GetFileNameWithoutExtension(fileInfo.FullName); //file names are version numbers
                    if (info.ApplicationVersion.CompareTo(new Version(version)) < 0)
                    {
                        info.PathToVersionFile = fileInfo.FullName; //save the latest version that was used before the current version of this application
                        info.ApplicationVersion = new Version(version);
                    }
                }

                if (string.IsNullOrEmpty(info.PathToVersionFile))
                    return null;
                else //open the file and read the path to the application folder
                {
                    string pathInFile = File.ReadAllText(info.PathToVersionFile);
                    FileAttributes attr = File.GetAttributes(pathInFile);

                    if (attr.HasFlag(FileAttributes.Directory))
                        info.PathToApplicationFolder = pathInFile;
                    else
                        info.PathToApplicationFolder = Path.GetDirectoryName(pathInFile);
                }

                return info;
            }
            catch (Exception ex)
            {
                Logger.AddLogEntry(Logger.LogEntryCategories.Error, "Exception while trying to get info for newest version", ex, "VersionControl");
                return null;
            }
        }

        /// <summary>
        /// Gets the path to the version that was used before the newest one available.
        /// </summary>
        /// <returns>The info of the application before or null.</returns>
        public static ApplicationPathInfo GetInfoForVersionBefore()
        {
            try
            {
                string directoryPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), FOLDER_PATH_NEW);

                if (!Directory.Exists(directoryPath))
                    return null;

                string[] files = Directory.GetFiles(directoryPath);

                if (files == null || files.Length == 0)
                    return null;

                var fileInfos = files.Select(f => new FileInfo(f));
                ApplicationPathInfo info = new ApplicationPathInfo();

                Version currentVersion = new Version("0.0.0.1");
                string pathToCurrentVersion = null;

                foreach (FileInfo fileInfo in fileInfos.OrderBy(f => f.LastWriteTime)) //sort order is important here to make sure to get the oldest file at first (which might already be the one we need) --> always use LastWriteTime because the original CreationDate gets lost when a file is copied
                {
                    string version = Path.GetFileNameWithoutExtension(fileInfo.FullName); //file names are version numbers
                    if (currentVersion.CompareTo(new Version(version)) < 0)
                    {
                        info.PathToVersionFile = pathToCurrentVersion; //save the latest version that was used before the current version of this application
                        pathToCurrentVersion = fileInfo.FullName;
                        info.ApplicationVersion = currentVersion;
                        currentVersion = new Version(version);
                    }
                }

                if (string.IsNullOrEmpty(info.PathToVersionFile))
                    return null;
                else //open the file and read the path to the application folder
                {
                    string pathInFile = File.ReadAllText(info.PathToVersionFile);
                    FileAttributes attr = File.GetAttributes(pathInFile);

                    if (attr.HasFlag(FileAttributes.Directory))
                        info.PathToApplicationFolder = pathInFile;
                    else
                        info.PathToApplicationFolder = Path.GetDirectoryName(pathInFile);
                }

                Logger.AddLogEntry(Logger.LogEntryCategories.Trace, "Returning info with PathToApplicationFolder: " + info.PathToApplicationFolder, null, "VersionControl.cs");
                return info;
            }
            catch (Exception ex)
            {
                Logger.AddLogEntry(Logger.LogEntryCategories.Error, "Exception while trying to get info for version before", ex, "VersionControl");
                return null;
            }
        }
    }

    public class ApplicationPathInfo
    {
        public Version ApplicationVersion { get; set; }
        public string PathToApplicationFolder { get; set; }
        public string PathToVersionFile { get; set; }

        public override string ToString()
        {
            return $"Version: {(ApplicationVersion != null ? ApplicationVersion.ToString() : "")}, Path to application: {PathToApplicationFolder}, Path to version file: {PathToVersionFile}";
        }
    }
}
