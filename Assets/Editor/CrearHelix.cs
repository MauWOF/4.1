using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering;
using System.IO;

public static class CrearHelix
{
    static readonly Color Fondo = new Color(0.035f, 0.06f, 0.13f);
    static readonly Color Turquesa = new Color(0.12f, 0.88f, 0.74f);
    static Font fuente;
    [InitializeOnLoadMethod] static void AlCargar()
    {
        EditorApplication.delayCall += () => {
            if (!File.Exists("Logs/helix-v3.txt") && !EditorApplication.isPlayingOrWillChangePlaymode) Ejecutar();
        };
    }
    [MenuItem("Helix/Crear escenas iniciales")]
    public static void Ejecutar()
    {
        Directory.CreateDirectory("Assets/Scenes");
        Directory.CreateDirectory("Assets/Materials/Helix");
        fuente = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        Material seguro = Material("Plataforma", Turquesa);
        Material peligro = Material("Peligro", new Color(1, 0.23f, 0.36f));
        Material columna = Material("Columna", new Color(0.18f, 0.24f, 0.37f));
        Material bola = Material("Pelota", new Color(1, 0.79f, 0.26f));
        Material llegada = Material("Meta", new Color(0.56f, 0.4f, 1));
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/HelixPartPrefab.prefab");
        var original = prefab.GetComponent<MeshFilter>().sharedMesh;
        // El recurso está acostado con una rotación y escala propias. Se hornean para reutilizarlo.
        Mesh m = Object.Instantiate(original);
        var vertices = m.vertices;
        float radio = 0, altura = float.MinValue;
        for (int i=0;i<vertices.Length;i++) {
            vertices[i] = prefab.transform.localToWorldMatrix.MultiplyPoint3x4(vertices[i]);
            radio = Mathf.Max(radio, new Vector2(vertices[i].x, vertices[i].z).magnitude);
            altura = Mathf.Max(altura, vertices[i].y);
        }
        for (int i=0;i<vertices.Length;i++) vertices[i] = new Vector3(vertices[i].x * 3.2f / radio, (vertices[i].y-altura)*0.7f, vertices[i].z*3.2f/radio);
        m.vertices=vertices; m.RecalculateNormals(); m.RecalculateBounds();
        string rutaMalla="Assets/Models/PlataformaAdaptada.asset";
        if (AssetDatabase.LoadAssetAtPath<Mesh>(rutaMalla)==null) AssetDatabase.CreateAsset(m,rutaMalla);
        else { Object.DestroyImmediate(m); m=AssetDatabase.LoadAssetAtPath<Mesh>(rutaMalla); }
        // Corrige la referencia de collider que falta en el paquete original.
        prefab.GetComponent<MeshCollider>().sharedMesh=original;
        EditorUtility.SetDirty(prefab);
        CrearJuego(m, seguro, peligro, columna, bola, llegada);
        CrearMenu(m,seguro,peligro,columna,bola);
        PlayerSettings.productName="Helix Jump";
        PlayerSettings.companyName="ProyectoFinal";
        PlayerSettings.defaultInterfaceOrientation=UIOrientation.Portrait;
        PlayerSettings.defaultScreenWidth=540; PlayerSettings.defaultScreenHeight=960;
        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android,"com.proyectofinal.helixjump");
        PlayerSettings.Android.minSdkVersion=AndroidSdkVersions.AndroidApiLevel26;
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android,ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures=AndroidArchitecture.ARM64;
        var rp=AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>("Assets/Settings/Mobile_RPAsset.asset");
        if(rp!=null) { GraphicsSettings.defaultRenderPipeline=rp; QualitySettings.renderPipeline=rp; }
        EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene("Assets/Scenes/Menu.unity",true),new EditorBuildSettingsScene("Assets/Scenes/Juego.unity",true)};
        AssetDatabase.SaveAssets();
        File.WriteAllText("Logs/helix-creado.txt","Escenas Menu y Juego creadas correctamente.");
        File.WriteAllText("Logs/helix-v2.txt","Torre con sectores de 30 grados.");
        File.WriteAllText("Logs/helix-v3.txt","Encuadre final y menu legible.");
        EditorSceneManager.OpenScene("Assets/Scenes/Menu.unity");
    }
    static Material Material(string nombre,Color color)
    {
        string ruta="Assets/Materials/Helix/"+nombre+".mat";
        var mat=AssetDatabase.LoadAssetAtPath<Material>(ruta);
        if(mat==null) { mat=new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(mat,ruta); }
        mat.color=color; mat.SetFloat("_Smoothness",0.25f); return mat;
    }
    static Camera Ambiente()
    {
        RenderSettings.ambientLight=new Color(0.65f,0.7f,0.85f);
        var cam=new GameObject("Camara principal",typeof(Camera),typeof(AudioListener)).GetComponent<Camera>();
        cam.tag="MainCamera"; cam.clearFlags=CameraClearFlags.SolidColor; cam.backgroundColor=Fondo;
        cam.transform.position=new Vector3(0,5.8f,-12f); cam.transform.rotation=Quaternion.Euler(25,0,0);
        cam.fieldOfView=53; cam.nearClipPlane=0.1f; cam.farClipPlane=100;
        var luz=new GameObject("Luz direccional",typeof(Light)).GetComponent<Light>();
        luz.type=LightType.Directional; luz.intensity=1.3f; luz.shadows=LightShadows.Soft; luz.transform.rotation=Quaternion.Euler(45,-25,0);
        return cam;
    }
    static GameObject Primitiva(string nombre,PrimitiveType tipo,Vector3 pos,Vector3 escala,Material material)
    {
        var obj=GameObject.CreatePrimitive(tipo); obj.name=nombre; obj.transform.position=pos; obj.transform.localScale=escala; obj.GetComponent<Renderer>().sharedMaterial=material; return obj;
    }
    static void Pieza(Mesh mesh,Transform padre,float y,float angulo,Material mat,bool peligrosa)
    {
        var obj=new GameObject(peligrosa?"Sector peligroso":"Sector seguro",typeof(MeshFilter),typeof(MeshRenderer),typeof(MeshCollider),typeof(Superficie));
        obj.transform.SetParent(padre,false); obj.transform.localPosition=Vector3.up*y; obj.transform.localRotation=Quaternion.Euler(0,angulo,0);
        obj.GetComponent<MeshFilter>().sharedMesh=mesh; obj.GetComponent<MeshRenderer>().sharedMaterial=mat;
        var col=obj.GetComponent<MeshCollider>(); col.sharedMesh=mesh; col.convex=true;
        obj.GetComponent<Superficie>().peligrosa=peligrosa;
    }
    static void CrearJuego(Mesh mesh,Material seguro,Material peligro,Material columna,Material bola,Material llegada)
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var cam=Ambiente();
        var partida=new GameObject("Control de partida",typeof(Partida),typeof(AudioSource)).GetComponent<Partida>();
        var torre=new GameObject("Torre giratoria",typeof(Rigidbody),typeof(GirarTorre));
        torre.GetComponent<Rigidbody>().isKinematic=true;
        torre.GetComponent<Rigidbody>().interpolation=RigidbodyInterpolation.Interpolate;
        torre.GetComponent<GirarTorre>().partida=partida;
        Primitiva("Columna central",PrimitiveType.Cylinder,new Vector3(0,-17.5f,0),new Vector3(1.2f,22,1.2f),columna).transform.SetParent(torre.transform,true);
        for(int piso=0;piso<12;piso++) {
            int hueco=(piso*3)%12;
            for(int sector=0;sector<12;sector++) {
                int distancia=(sector-hueco+12)%12;
                if(distancia<3) continue;
                bool rojo=piso>1 && (distancia==6 || distancia==7);
                Pieza(mesh,torre.transform,-piso*3.2f,sector*30+piso*23,rojo?peligro:seguro,rojo);
            }
        }
        var meta=Primitiva("Plataforma de llegada",PrimitiveType.Cylinder,new Vector3(0,-38.4f,0),new Vector3(6.5f,0.2f,6.5f),llegada);
        meta.transform.SetParent(torre.transform,true); meta.AddComponent<Superficie>().meta=true;
        var pelota=Primitiva("Pelota",PrimitiveType.Sphere,new Vector3(0.75f,1.8f,-2.3f),Vector3.one*0.48f,bola);
        var rb=pelota.AddComponent<Rigidbody>(); rb.mass=1; rb.interpolation=RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;
        rb.constraints=RigidbodyConstraints.FreezePositionX|RigidbodyConstraints.FreezePositionZ|RigidbodyConstraints.FreezeRotation;
        pelota.AddComponent<Pelota>().partida=partida; partida.pelota=rb;
        cam.gameObject.AddComponent<CamaraSeguimiento>().pelota=pelota.transform;
        partida.rebote=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/mixkit-arcade-game-jump-coin-216.wav");
        partida.victoria=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/mixkit-instant-win-2021.wav");
        partida.derrota=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/mixkit-small-win-2020.wav");
        var ui=Canvas();
        Texto(ui,"NIVEL 01",22,new Vector2(0.5f,0.94f),new Vector2(260,45),Turquesa);
        partida.textoPuntos=Texto(ui,"00",65,new Vector2(0.5f,0.87f),new Vector2(300,100),Color.white);
        Boton(ui,"II",new Vector2(0.87f,0.94f),new Vector2(65,65),partida.Pausar);
        partida.textoAyuda=Texto(ui,"DESLIZA PARA GIRAR\nBusca los huecos. Evita el rojo.",23,new Vector2(0.5f,0.15f),new Vector2(490,100),Color.white);
        var barra=Panel(ui,"Progreso",new Color(1,1,1,0.12f),new Vector2(0.5f,0.07f),new Vector2(390,12));
        var relleno=Panel(barra.transform,"Avance",Turquesa,new Vector2(0.5f,0.5f),new Vector2(390,12));
        var slider=barra.AddComponent<Slider>(); slider.fillRect=(RectTransform)relleno.transform; slider.targetGraphic=relleno.GetComponent<Image>(); slider.interactable=false; slider.transition=Selectable.Transition.None;
        slider.value=0; partida.progreso=slider;
        partida.panelPausa=Panel(ui,"Pausa",new Color(0.02f,0.035f,0.08f,0.97f),new Vector2(0.5f,0.5f),new Vector2(500,530));
        Texto(partida.panelPausa.transform,"PAUSA",45,new Vector2(0.5f,0.82f),new Vector2(450,70),Color.white);
        Boton(partida.panelPausa.transform,"CONTINUAR",new Vector2(0.5f,0.58f),new Vector2(390,75),partida.Pausar);
        Boton(partida.panelPausa.transform,"REINICIAR",new Vector2(0.5f,0.38f),new Vector2(390,75),partida.Reiniciar);
        Boton(partida.panelPausa.transform,"MENÚ",new Vector2(0.5f,0.18f),new Vector2(390,75),partida.Menu);
        partida.panelResultado=Panel(ui,"Resultado",new Color(0.02f,0.035f,0.08f,0.97f),new Vector2(0.5f,0.5f),new Vector2(500,450));
        partida.textoResultado=Texto(partida.panelResultado.transform,"RESULTADO",32,new Vector2(0.5f,0.77f),new Vector2(460,100),Color.white);
        Boton(partida.panelResultado.transform,"VOLVER A JUGAR",new Vector2(0.5f,0.43f),new Vector2(390,75),partida.Reiniciar);
        Boton(partida.panelResultado.transform,"MENÚ",new Vector2(0.5f,0.2f),new Vector2(390,75),partida.Menu);
        partida.panelPausa.SetActive(false); partida.panelResultado.SetActive(false);
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(),"Assets/Scenes/Juego.unity");
    }
    static void CrearMenu(Mesh mesh,Material seguro,Material peligro,Material columna,Material bola)
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single); Ambiente();
        var decoracion=new GameObject("Torre de portada");
        for(int i=0;i<5;i++) for(int s=0;s<9;s++) Pieza(mesh,decoracion.transform,-i*2.2f,s*30+i*25,s>6?peligro:seguro,false);
        Primitiva("Columna",PrimitiveType.Cylinder,new Vector3(0,-4,0),new Vector3(1.2f,7,1.2f),columna);
        Primitiva("Pelota de portada",PrimitiveType.Sphere,new Vector3(0.75f,0.6f,-2.3f),Vector3.one*0.48f,bola);
        var menu=new GameObject("Menu principal",typeof(MenuPrincipal)).GetComponent<MenuPrincipal>();
        var ui=Canvas();
        Panel(ui,"Fondo de botones",new Color(0.035f,0.06f,0.13f,0.94f),new Vector2(0.5f,0.18f),new Vector2(540,350));
        Texto(ui,"HELIX\nJUMP",72,new Vector2(0.5f,0.81f),new Vector2(500,210),Color.white);
        Texto(ui,"GIRA · REBOTA · DESCIENDE",19,new Vector2(0.5f,0.66f),new Vector2(500,45),Turquesa);
        Boton(ui,"JUGAR",new Vector2(0.5f,0.27f),new Vector2(410,85),menu.Jugar);
        Boton(ui,"CÓMO JUGAR",new Vector2(0.5f,0.16f),new Vector2(410,65),menu.Ayuda);
        Texto(ui,"PROYECTO FINAL · FASE 1",17,new Vector2(0.5f,0.05f),new Vector2(450,40),new Color(0.65f,0.73f,0.86f));
        menu.instrucciones=Panel(ui,"Instrucciones",new Color(0.025f,0.045f,0.1f,0.99f),new Vector2(0.5f,0.5f),new Vector2(500,620));
        Texto(menu.instrucciones.transform,"CÓMO JUGAR",37,new Vector2(0.5f,0.85f),new Vector2(470,70),Turquesa);
        Texto(menu.instrucciones.transform,"Desliza el dedo para girar la torre.\n\nLa pelota rebota automáticamente.\nBusca los huecos para bajar.\n\nEvita las plataformas rojas\ny llega hasta la base.\n\nEn computadora: arrastra el ratón\no usa las flechas izquierda y derecha.",24,new Vector2(0.5f,0.49f),new Vector2(450,350),Color.white);
        Boton(menu.instrucciones.transform,"ENTENDIDO",new Vector2(0.5f,0.12f),new Vector2(390,70),menu.Ayuda);
        menu.instrucciones.SetActive(false);
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(),"Assets/Scenes/Menu.unity");
    }
    static Transform Canvas()
    {
        var canvas=new GameObject("Interfaz movil",typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
        canvas.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
        var escala=canvas.GetComponent<CanvasScaler>(); escala.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize; escala.referenceResolution=new Vector2(540,960); escala.matchWidthOrHeight=0.5f;
        var safe=new GameObject("Area segura",typeof(RectTransform),typeof(AreaSegura)); safe.transform.SetParent(canvas.transform,false);
        var rect=(RectTransform)safe.transform; rect.anchorMin=Vector2.zero; rect.anchorMax=Vector2.one; rect.offsetMin=rect.offsetMax=Vector2.zero;
        new GameObject("EventSystem",typeof(EventSystem),typeof(InputSystemUIInputModule));
        return safe.transform;
    }
    static GameObject Elemento(Transform padre,string nombre,Vector2 ancla,Vector2 tamano)
    {
        var obj=new GameObject(nombre,typeof(RectTransform)); obj.transform.SetParent(padre,false);
        var rect=(RectTransform)obj.transform; rect.anchorMin=rect.anchorMax=ancla; rect.sizeDelta=tamano; rect.anchoredPosition=Vector2.zero; return obj;
    }
    static GameObject Panel(Transform padre,string nombre,Color color,Vector2 ancla,Vector2 tamano)
    {
        var obj=Elemento(padre,nombre,ancla,tamano); obj.AddComponent<Image>().color=color; return obj;
    }
    static Text Texto(Transform padre,string contenido,int tamano,Vector2 ancla,Vector2 medidas,Color color)
    {
        var text=Elemento(padre,contenido.Split('\n')[0],ancla,medidas).AddComponent<Text>();
        text.text=contenido; text.font=fuente; text.fontSize=tamano; text.color=color; text.alignment=TextAnchor.MiddleCenter; text.raycastTarget=false; return text;
    }
    static void Boton(Transform padre,string titulo,Vector2 ancla,Vector2 tamano,UnityAction accion)
    {
        var obj=Panel(padre,titulo,Turquesa,ancla,tamano); var boton=obj.AddComponent<Button>(); boton.targetGraphic=obj.GetComponent<Image>();
        UnityEditor.Events.UnityEventTools.AddPersistentListener(boton.onClick,accion);
        Texto(obj.transform,titulo,25,new Vector2(0.5f,0.5f),tamano,Fondo);
    }
}
