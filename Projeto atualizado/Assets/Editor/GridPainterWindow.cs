using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class GridPainterWindow : EditorWindow
{
    public List<GameObject> personagens = new List<GameObject>();
    public List<GameObject> obstaculos = new List<GameObject>();
    public List<GameObject> chaoGelo = new List<GameObject>();

    private enum Categoria { Personagens, Obstaculo, ChaoGelo }
    private Categoria categoriaAtiva = Categoria.Personagens;
    private int indiceSelecionado = 0;

    // Dados do Preview (Sem instanciar GameObjects temporários)
    private Vector3 previewGridPos;
    private bool desenharPreview;

    [MenuItem("Huguinho/Editor de Grid")]
    public static void ShowWindow()
    {
        GetWindow<GridPainterWindow>("Editor de Grid");
    }

    private void OnGUI()
    {
        SerializedObject so = new SerializedObject(this);
        EditorGUILayout.PropertyField(so.FindProperty("personagens"), true);
        EditorGUILayout.PropertyField(so.FindProperty("obstaculos"), true);
        EditorGUILayout.PropertyField(so.FindProperty("chaoGelo"), true);
        so.ApplyModifiedProperties();

        EditorGUILayout.Space(10);
        EditorGUILayout.HelpBox("ATALHOS NA SCENE VIEW:\n" +
            "Teclas [1, 2, 3]: Muda categoria\n" +
            "Scroll Mouse + SHIFT: Gira seleção de Prefabs\n" +
            "SHIFT + Mouse Move: Mostra Preview\n" +
            "SHIFT + Clique Esquerdo: Instancia no Grid (Segure e arraste para pintar)\n" +
            "SHIFT + Clique Direito: Apaga objeto", MessageType.Info);

        EditorGUILayout.LabelField($"Categoria Ativa: {categoriaAtiva}", EditorStyles.boldLabel);
        GameObject ativo = ObterPrefabAtivo();
        EditorGUILayout.LabelField($"Selecionado [{indiceSelecionado}]: {(ativo != null ? ativo.name : "Nenhum")}");
    }

    private void OnEnable() => SceneView.duringSceneGui += OnSceneGUI;

    private void OnDisable() => SceneView.duringSceneGui -= OnSceneGUI;

    private void OnSceneGUI(SceneView sceneView)
    {
        Event e = Event.current;

        // Repinta a cena ao mover o mouse para atualizar o preview instantaneamente
        if (e.type == EventType.MouseMove)
        {
            sceneView.Repaint();
        }

        // Atalhos de teclado (Foco precisa estar na Scene View)
        if (e.type == EventType.KeyDown)
        {
            if (e.keyCode == KeyCode.Alpha1)
            {
                categoriaAtiva = Categoria.Personagens;
                indiceSelecionado = 0;
                e.Use();
                Repaint(); // Atualiza a janela do Editor
                sceneView.Repaint(); // Atualiza a Scene View
            }
            else if (e.keyCode == KeyCode.Alpha2)
            {
                categoriaAtiva = Categoria.Obstaculo;
                indiceSelecionado = 0;
                e.Use();
                Repaint();
                sceneView.Repaint();
            }
            else if (e.keyCode == KeyCode.Alpha3)
            {
                categoriaAtiva = Categoria.ChaoGelo;
                indiceSelecionado = 0;
                e.Use();
                Repaint();
                sceneView.Repaint();
            }
        }

        // Navegação de Prefabs com Scroll + Shift
        if (e.type == EventType.ScrollWheel && e.shift)
        {
            int max = ObterTamanhoListaAtiva();
            if (max > 0)
            {
                indiceSelecionado += e.delta.y > 0 ? 1 : -1;
                indiceSelecionado = (indiceSelecionado % max + max) % max;
                e.Use();
                Repaint();
            }
        }

        desenharPreview = false;

        // Pintura no Grid com Shift
        if (e.shift)
        {
            int controlID = GUIUtility.GetControlID(FocusType.Passive);
            HandleUtility.AddDefaultControl(controlID);

            Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
            Vector3 hitPoint = Vector3.zero;
            Vector3 hitNormal = Vector3.up; 
            bool hitValido = false;

            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                hitPoint = hit.point;
                hitNormal = hit.normal; 
                hitValido = true;
            }
            else
            {
                Plane planoY = new Plane(Vector3.up, Vector3.zero);
                if (planoY.Raycast(ray, out float distancia))
                {
                    hitPoint = ray.GetPoint(distancia);
                    hitNormal = Vector3.up;
                    hitValido = true;
                }
            }

            if (hitValido)
            {
                Vector3 offsetPos = hitPoint + (hitNormal * 0.5f);

                previewGridPos = new Vector3(
                    Mathf.Round(offsetPos.x),
                    Mathf.Round(offsetPos.y - 0.5f) + 0.5f,
                    Mathf.Round(offsetPos.z)
                );

                desenharPreview = true;

                // Clique Esquerdo: Instancia
                if ((e.type == EventType.MouseDown || e.type == EventType.MouseDrag) && e.button == 0)
                {
                    GameObject prefab = ObterPrefabAtivo();
                    if (prefab != null && !PosicaoOcupada(previewGridPos))
                    {
                        GameObject clone = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                        clone.transform.position = previewGridPos;
                        Undo.RegisterCreatedObjectUndo(clone, "Pintar Bloco");
                    }
                    e.Use();
                }
                // Clique Direito: Apaga
                else if ((e.type == EventType.MouseDown || e.type == EventType.MouseDrag) && e.button == 1 && hit.collider != null)
                {
                    Undo.DestroyObjectImmediate(hit.collider.gameObject);
                    e.Use();
                }
            }
        }

        // Desenha o Gizmo de Preview em formato de Wireframe na Scene View
        if (desenharPreview && ObterPrefabAtivo() != null)
        {
            Handles.color = Color.cyan; // Cor da linha do Gizmo
            Handles.DrawWireCube(previewGridPos, Vector3.one); // Desenha as linhas do cubo 1x1x1
            sceneView.Repaint();
        }
    }

    private bool PosicaoOcupada(Vector3 pos)
    {
        return Physics.CheckBox(pos, new Vector3(0.4f, 0.4f, 0.4f));
    }

    private int ObterTamanhoListaAtiva()
    {
        return categoriaAtiva switch
        {
            Categoria.Personagens => personagens.Count,
            Categoria.Obstaculo => obstaculos.Count,
            Categoria.ChaoGelo => chaoGelo.Count,
            _ => 0
        };
    }

    private GameObject ObterPrefabAtivo()
    {
        List<GameObject> lista = categoriaAtiva switch
        {
            Categoria.Personagens => personagens,
            Categoria.Obstaculo => obstaculos,
            Categoria.ChaoGelo => chaoGelo,
            _ => null
        };

        if (lista != null && lista.Count > 0 && indiceSelecionado < lista.Count)
        {
            return lista[indiceSelecionado];
        }
        return null;
    }
}
