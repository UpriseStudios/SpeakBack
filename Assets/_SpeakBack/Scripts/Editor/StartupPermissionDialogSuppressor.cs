using System.IO;
using System.Linq;
using System.Xml.Linq;
using UnityEditor.Android;

namespace Uprise.SpeakBack.EditorTools
{
    /// <summary>
    /// Adds <c>unityplayer.SkipPermissionsDialog</c> to the Android manifest so Unity never shows the OS
    /// microphone prompt on its own. The game explains first and then asks through
    /// MicrophonePermissionService (Phase 1 §8 step 1, Master §28, P00 finding B2).
    /// </summary>
    public sealed class StartupPermissionDialogSuppressor : IPostGenerateGradleAndroidProject
    {
        private const string MetaDataName = "unityplayer.SkipPermissionsDialog";
        private static readonly XNamespace Android = "http://schemas.android.com/apk/res/android";

        public int callbackOrder => 1;

        public void OnPostGenerateGradleAndroidProject(string unityLibraryPath)
        {
            var manifestPath = Path.Combine(unityLibraryPath, "src", "main", "AndroidManifest.xml");
            var document = XDocument.Load(manifestPath);
            var application = document.Root?.Element("application");
            if (application == null) return;

            var existing = application.Elements("meta-data").FirstOrDefault(e => (string)e.Attribute(Android + "name") == MetaDataName);
            if (existing == null)
            {
                application.Add(new XElement("meta-data",
                    new XAttribute(Android + "name", MetaDataName),
                    new XAttribute(Android + "value", "true")));
            }
            else
            {
                existing.SetAttributeValue(Android + "value", "true");
            }

            document.Save(manifestPath);
        }
    }
}
