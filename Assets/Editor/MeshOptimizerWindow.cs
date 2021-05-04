using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public sealed class MeshOptimizerWindow : EditorWindow
{
    private GameObject _go;

    [MenuItem("Window/PLATEAU Mesh Optimizer")]
    public static void ShowWindow()
    {
        var window = GetWindow<MeshOptimizerWindow>();
        window.titleContent = new GUIContent("PLATEAU Mesh Optimizer");
        window.position = new Rect(10, 10, 300, 300);
    }

    public void OnGUI()
    {
        GUILayout.Label("FBXをインポートしてシーンに置いてからTargetにセットしてください。");

        _go = EditorGUILayout.ObjectField("Target", _go, typeof(GameObject)) as GameObject;

        if (GUILayout.Button("変換", GUILayout.Height(40)))
        {
            Debug.Log($"Child Count: {_go.transform.childCount}");

            var t0 = Time.realtimeSinceStartup;

            var parent = new GameObject(_go.name);

            AssetDatabase.StartAssetEditing();

            foreach (Transform t in _go.transform)
            {
                MergeMesh(t, parent.transform);
            }

            AssetDatabase.StopAssetEditing();

            var time = Time.realtimeSinceStartup - t0;

            Debug.Log($"Done: {time}sec");
        }
    }

    private void MergeMesh(Transform t, Transform parent)
    {
        var newMesh = new Mesh();
        var vertices = new List<Vector3>();
        var indexes = new List<int>();
        var uvs = new List<Vector2>();
        var index = 0;

        Material mat = null;

        foreach (Transform child in t)
        {
            var meshFilter = child.GetComponent<MeshFilter>();
            var mesh = meshFilter.mesh;

            // FIXME 最後に取得したマテリアルをセットするが、意図したマテリアルになる保証はない。
            var meshRenderer = child.GetComponent<MeshRenderer>();
            mat = meshRenderer.material;

            vertices.AddRange(mesh.vertices);

            if (mesh.uv.Length == 0)
            {
                // UVが無いモデルは0で埋める。
                for (var i = 0; i < mesh.vertices.Length; i++)
                    uvs.Add(Vector2.zero);
            }
            else
            {
                uvs.AddRange(mesh.uv);
            }

            var tris = mesh.triangles;
            foreach (var i in tris) indexes.Add(i + index);

            index += mesh.vertices.Length;
        }

        newMesh.vertices = vertices.ToArray();
        newMesh.triangles = indexes.ToArray();
        newMesh.uv = uvs.ToArray();
        newMesh.RecalculateNormals();
        CreateObject(newMesh, mat, t.name, parent);
    }

    // GameObject生成
    private void CreateObject(Mesh mesh, Material material, string objName, Transform parent)
    {
        CreateAsset(mesh, objName, $"{parent.name}/mesh");
        CreateAsset(material, objName, $"{parent.name}/material");

        var go = new GameObject(objName);
        go.transform.localRotation = Quaternion.Euler(-90, 0, 0);
        go.transform.SetParent(parent);
        var mf = go.AddComponent<MeshFilter>();
        mf.sharedMesh = mesh;
        var mr = go.AddComponent<MeshRenderer>();
        mr.sharedMaterial = material;
    }

    // アセット生成
    private void CreateAsset(Object obj, string assetName, string directory)
    {
        if (AssetDatabase.Contains(obj)) return;

        var dir = Path.Combine(Application.dataPath, directory);
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

        var fileName = assetName + ".asset";

        var path = Path.Combine(Application.dataPath, directory, fileName);

        var cnt = 1;
        while (File.Exists(path))
        {
            fileName = $"{assetName} {cnt}.asset";
            path = Path.Combine(Application.dataPath, directory, fileName);
            cnt++;
        }

        AssetDatabase.CreateAsset(obj, Path.Combine("Assets", directory, fileName));
    }
}