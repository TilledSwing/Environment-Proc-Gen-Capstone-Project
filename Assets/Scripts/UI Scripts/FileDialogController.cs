using System;
using System.IO;
using SFB;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using UnityEngine.UI.Extensions;

public class FileDialogController : MonoBehaviour
{
    public void OpenAssetFile()
    {
        var paths = StandaloneFileBrowser.OpenFolderPanel("Open Folder", "", false);

        if (paths.Length > 0)
        {
            for (int i = 0; i < paths.Length; i++)
            {
                string path = paths[i];
                string extension = Path.GetExtension(path).ToLower();

                switch (extension)
                {
                    case ".jpg":
                    case ".png":
                    case ".jpeg":

                        break;
                    case ".fbx":
                    case ".obj":

                        break;
                    case ".mtl":

                        break;
                }
            }
        }
    }
}
