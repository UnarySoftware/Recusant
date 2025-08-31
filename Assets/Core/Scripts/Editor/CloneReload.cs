#if UNITY_EDITOR

using UnityEditor;
using System.IO;
using System.Diagnostics;
using System.Threading;

namespace Core.Editor
{
    [InitializeOnLoad]
    public class CloneReload
    {
        static CloneReload()
        {
            LastTime = EditorApplication.timeSinceStartup;
            ThisProcess = Process.GetCurrentProcess();
            ReloadFile = "../" + ThisProcess.Id + ".reload";
            EditorApplication.update += OnUpdate;
        }

        public static double LastTime;
        public static double Timer;
        public static double FileCheck = 2.0;
        public static Process ThisProcess;
        public static string ReloadFile;

        [MenuItem("Core/Clone Reload %r")]
        static void Reload()
        {
            if (!File.Exists(ReloadFile))
            {
                File.Create(ReloadFile).Dispose();
            }
            AssetDatabase.Refresh();
        }

        static void OnUpdate()
        {
            Timer += EditorApplication.timeSinceStartup - LastTime;

            if (Timer >= FileCheck)
            {
                Timer = 0.0;

                string[] Files = Directory.GetFiles("..", "*.reload", SearchOption.TopDirectoryOnly);

                bool ShouldReload = false;

                foreach (string TargetFile in Files)
                {
                    int Pid = int.Parse(Path.GetFileNameWithoutExtension(TargetFile));

                    if (Pid != ThisProcess.Id)
                    {
                        ShouldReload = true;
                        int CountUp = 0;
                        bool ShouldDelete = true;
                        FileInfo Info = new(TargetFile);
                        while (Info.IsLocked())
                        {
                            Thread.Sleep(200);
                            CountUp++;
                            if (CountUp > 4)
                            {
                                ShouldReload = false;
                                ShouldDelete = false;
                                break;
                            }
                        }
                        if (ShouldDelete)
                        {
                            File.Delete(TargetFile);
                        }
                    }
                }

                if (ShouldReload)
                {
                    AssetDatabase.Refresh();
                }
            }

            LastTime = EditorApplication.timeSinceStartup;
        }
    }
}

#endif
