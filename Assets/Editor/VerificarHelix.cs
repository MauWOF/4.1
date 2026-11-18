using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;

[InitializeOnLoad]
public static class VerificarHelix
{
    static int fase;
    static double inicio;
    static float menorY, mayorY;
    static bool reboto;
    static VerificarHelix() {
        EditorApplication.playModeStateChanged += estado => {
            if(estado==PlayModeStateChange.EnteredEditMode && SessionState.GetBool("HelixVolverMenu",false)) {
                SessionState.SetBool("HelixVolverMenu",false);
                EditorSceneManager.OpenScene("Assets/Scenes/Menu.unity");
            }
        };
        EditorApplication.update += Actualizar;
    }
    [MenuItem("Helix/Verificar funcionamiento")]
    public static void Preparar() {
        if(EditorApplication.isPlayingOrWillChangePlaymode || File.Exists("Logs/verificacion-helix.txt") || !File.Exists("Logs/helix-v2.txt")) return;
        Directory.CreateDirectory("Evidencias");
        EditorSceneManager.OpenScene("Assets/Scenes/Menu.unity");
        Capturar("Evidencias/Menu.png");
        EditorSceneManager.OpenScene("Assets/Scenes/Juego.unity");
        Capturar("Evidencias/Juego.png");
        SessionState.SetBool("HelixVerificar",true);
        EditorApplication.isPlaying=true;
    }
    [MenuItem("Helix/Guardar vistas del proyecto")]
    public static void GuardarVistas() {
        Directory.CreateDirectory("Evidencias");
        EditorSceneManager.OpenScene("Assets/Scenes/Juego.unity"); Capturar("Evidencias/Juego.png");
        EditorSceneManager.OpenScene("Assets/Scenes/Menu.unity"); Capturar("Evidencias/Menu.png");
    }
    static void Capturar(string ruta) {
        var cam=Camera.main;
        var canvas=Object.FindAnyObjectByType<Canvas>();
        canvas.renderMode=RenderMode.ScreenSpaceCamera; canvas.worldCamera=cam; canvas.planeDistance=1;
        Canvas.ForceUpdateCanvases();
        var rt=new RenderTexture(540,960,24);
        cam.targetTexture=rt; cam.Render(); RenderTexture.active=rt;
        var tex=new Texture2D(540,960,TextureFormat.RGB24,false);
        tex.ReadPixels(new Rect(0,0,540,960),0,0); tex.Apply(); File.WriteAllBytes(ruta,tex.EncodeToPNG());
        RenderTexture.active=null; cam.targetTexture=null; Object.DestroyImmediate(rt); Object.DestroyImmediate(tex);
        canvas.renderMode=RenderMode.ScreenSpaceOverlay; canvas.worldCamera=null;
    }
    static void Actualizar() {
        if(!SessionState.GetBool("HelixVerificar",false) || !EditorApplication.isPlaying || EditorApplication.isCompiling) return;
        var p=Object.FindAnyObjectByType<Partida>();
        if(p==null) return;
        if(fase==0) { inicio=EditorApplication.timeSinceStartup; menorY=100; mayorY=-100; fase=1; }
        if(fase==1) {
            menorY=Mathf.Min(menorY,p.pelota.position.y); mayorY=Mathf.Max(mayorY,p.pelota.position.y);
            if(p.pelota.linearVelocity.y>5) reboto=true;
            if(EditorApplication.timeSinceStartup-inicio<4) return;
            File.WriteAllText("Logs/verificacion-helix.txt","Rebote fisico: "+reboto+"; rango Y: "+menorY+" a "+mayorY+"\n");
            p.Pausar();
            File.AppendAllText("Logs/verificacion-helix.txt","Pausa: "+(!p.Jugando && Time.timeScale==0 && p.panelPausa.activeSelf)+"\n");
            p.Pausar();
            var peligro=System.Array.Find(Object.FindObjectsByType<Superficie>(),s=>s.peligrosa);
            Vector3 posicion=Quaternion.Euler(0,peligro.transform.eulerAngles.y,0)*new Vector3(1.65f,0,1.65f);
            posicion.y=peligro.transform.position.y+1;
            p.pelota.constraints=RigidbodyConstraints.FreezeRotation;
            p.pelota.position=posicion; p.pelota.linearVelocity=Vector3.down*2;
            inicio=EditorApplication.timeSinceStartup; fase=2;
        } else if(fase==2 && EditorApplication.timeSinceStartup-inicio>1.5) {
            File.AppendAllText("Logs/verificacion-helix.txt","Colision roja/derrota: "+(!p.Jugando && p.panelResultado.activeSelf)+"\n");
            p.Reiniciar(); fase=3; inicio=EditorApplication.timeSinceStartup;
        } else if(fase==3 && EditorApplication.timeSinceStartup-inicio>1) {
            p.pelota.position=new Vector3(0.75f,-36.9f,-2.3f); p.pelota.linearVelocity=Vector3.down*2;
            inicio=EditorApplication.timeSinceStartup; fase=4;
        } else if(fase==4 && EditorApplication.timeSinceStartup-inicio>1.5) {
            File.AppendAllText("Logs/verificacion-helix.txt","Meta/victoria: "+(!p.Jugando && p.textoResultado.text.Contains("COMPLETADA"))+"\n");
            p.Menu(); fase=5;
        }
    }
    [InitializeOnLoadMethod] static void Escuchar() {
        EditorApplication.update += () => {
            if(fase==5 && SceneManager.GetActiveScene().name=="Menu") {
                File.AppendAllText("Logs/verificacion-helix.txt","Regreso a Menu: True\n");
                SessionState.SetBool("HelixVerificar",false); fase=6;
                SessionState.SetBool("HelixVolverMenu",true); EditorApplication.isPlaying=false;
            }
        };
    }
}
