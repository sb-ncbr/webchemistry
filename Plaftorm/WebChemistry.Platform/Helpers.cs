using System;
using System.IO;

namespace WebChemistry.Platform
{
    /// <summary>
    /// Helpers.
    /// </summary>
    public class Helpers
    {
        /// <summary>
        /// Gets the time string in pretty format.
        /// </summary>
        /// <param name="elapsed"></param>
        /// <returns></returns>
        public static string GetTimeString(TimeSpan elapsed)
        {
            return
                elapsed.Days > 0
                ? elapsed.ToString(@"d\.hh\hmm\mss\s")
                : elapsed.Hours > 0
                ? elapsed.ToString(@"h\hmm\mss\s")
                : elapsed.Minutes > 0
                ? elapsed.ToString(@"m\mss\s")
                : elapsed.ToString(@"s\s");
        }

        /// <summary>
        /// Attemps to delete an entity. Returns true if delete, false if exception or the entity does not exist.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static bool DeleteEntity(EntityId id)
        {
            try
            {
                var path = id.GetEntityPath();
                if (File.Exists(path))
                {
                    File.Delete(path);
                    return true;
                }
                else if (Directory.Exists(path))
                {
                    Directory.Delete(path, true);
                    return true;
                }

                return false;
            }
            catch { return false; }
        }

        /// <summary>
        /// Copy directories.
        /// </summary>
        /// <param name="sourceDirName"></param>
        /// <param name="destDirName"></param>
        /// <param name="copySubDirs"></param>
        public static void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs)
        {
            DirectoryInfo dir = new DirectoryInfo(sourceDirName);
            DirectoryInfo[] dirs = dir.GetDirectories();

            if (!dir.Exists)
            {
                throw new DirectoryNotFoundException(
                    "Source directory does not exist or could not be found: "
                    + sourceDirName);
            }

            if (!Directory.Exists(destDirName))
            {
                Directory.CreateDirectory(destDirName);
            }

            FileInfo[] files = dir.GetFiles();
            foreach (FileInfo file in files)
            {
                string temppath = Path.Combine(destDirName, file.Name);
                file.CopyTo(temppath, false);
            }

            if (copySubDirs)
            {
                foreach (DirectoryInfo subdir in dirs)
                {
                    string temppath = Path.Combine(destDirName, subdir.Name);
                    DirectoryCopy(subdir.FullName, temppath, copySubDirs);
                }
            }
        }
    }
}
