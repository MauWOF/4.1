using UnityEngine;
using UnityEditor;
using UnityEditor.Build.Reporting;
using System.IO;

[InitializeOnLoad]
public static class CompilarAndroid
{
    static CompilarAndroid() { EditorApplication.update += RevisarSolicitud; }
    [MenuItem("Helix/Compilar APK de prueba")]
    public static void Solicitar() { SessionState.SetBool("HelixCompilarAndroid",true); }
    static void RevisarSolicitud()
    {
        if(!SessionState.GetBool("HelixCompilarAndroid",false) || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating || BuildPipeline.isBuildingPlayer) return;
        if(EditorUserBuildSettings.activeBuildTarget!=BuildTarget.Android) {
            EditorUserBuildSettings.SwitchActiveBuildTargetAsync(BuildTargetGroup.Android,BuildTarget.Android);
            return;
        }
        SessionState.SetBool("HelixCompilarAndroid",false);
        Directory.CreateDirectory("Builds/Android");
        EditorUserBuildSettings.buildAppBundle=false;
        var informe=BuildPipeline.BuildPlayer(new BuildPlayerOptions {
            scenes=new[]{"Assets/Scenes/Menu.unity","Assets/Scenes/Juego.unity"},
            locationPathName="Builds/Android/HelixJump.apk", target=BuildTarget.Android, options=BuildOptions.None
        });
        Debug.Log(informe.summary.result+"\nErrores: "+informe.summary.totalErrors+"\nTiempo: "+informe.summary.totalTime);
    }
}
