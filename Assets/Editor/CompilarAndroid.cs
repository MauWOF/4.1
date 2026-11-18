using UnityEngine;
using UnityEditor;
using UnityEditor.Build.Reporting;
using System.IO;

[InitializeOnLoad]
public static class CompilarAndroid
{
    static CompilarAndroid() { EditorApplication.update += RevisarSolicitud; }
    [MenuItem("Helix/Compilar APK de prueba")]
    public static void Solicitar() { File.WriteAllText("Logs/solicitud-apk.txt","Compilar"); }
    static void RevisarSolicitud()
    {
        if(!File.Exists("Logs/solicitud-apk.txt") || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating || BuildPipeline.isBuildingPlayer) return;
        if(File.ReadAllText("Logs/solicitud-apk.txt").Trim()!="Compilar") return;
        if(EditorUserBuildSettings.activeBuildTarget!=BuildTarget.Android) {
            VerificarHelix.GuardarVistas();
            EditorUserBuildSettings.SwitchActiveBuildTargetAsync(BuildTargetGroup.Android,BuildTarget.Android);
            return;
        }
        File.WriteAllText("Logs/solicitud-apk.txt","Procesada");
        Directory.CreateDirectory("Builds/Android");
        EditorUserBuildSettings.buildAppBundle=false;
        var informe=BuildPipeline.BuildPlayer(new BuildPlayerOptions {
            scenes=new[]{"Assets/Scenes/Menu.unity","Assets/Scenes/Juego.unity"},
            locationPathName="Builds/Android/HelixJump.apk", target=BuildTarget.Android, options=BuildOptions.None
        });
        File.WriteAllText("Logs/compilacion-android.txt",informe.summary.result+"\nErrores: "+informe.summary.totalErrors+"\nTiempo: "+informe.summary.totalTime);
    }
}
