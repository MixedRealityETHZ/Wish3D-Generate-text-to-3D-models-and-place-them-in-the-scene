using System.Collections.Generic;
using System.Text;
using System;
using UnityEngine;
using UnityEngine.XR.MagicLeap;
using UnityEngine.UI;
using System.Collections;
using System.IO;
using System.IO.Compression;
using UnityEngine.Networking;
using Dummiesman;
using Newtonsoft.Json;
using TMPro;



public class VoiceIntentsExample : MonoBehaviour
{
    [SerializeField, Tooltip("Configuration file that holds list of voice commands.")]
    private MLVoiceIntentsConfiguration _voiceConfiguration;

    [SerializeField, Tooltip("default game object")]
    private GameObject defaultGameObject;

    [SerializeField, Tooltip("mario game object")]
    private GameObject mario_box;

    [SerializeField, Tooltip("yes_sound")]
    public AudioSource yes;

    [SerializeField, Tooltip("3d_object_ready_sound")]
    public AudioSource ready_sound;

    [SerializeField, Tooltip("end_listening_sound")]
    public AudioSource end_listening;

    [SerializeField, Tooltip("listening text")]
    public TextMeshPro listening_text;

    [SerializeField, Tooltip("request_received")]
    public AudioSource request_received;

    [SerializeField, Tooltip("camera")]
    public Transform main_camera;

    private AudioClip clip;

    private GameObject copiedObject;
    private byte[] bytes;
    private string fastApiUrl = "http://2049.ch/audio_to_obj"; // "https://rare-moons-count.loca.lt/generate-mesh"; // 
    private readonly MLPermissions.Callbacks permissionCallbacks = new MLPermissions.Callbacks();

    private bool activate_surprise_box = false;
    private float mario_rotation_speed = 30f;

    [System.Serializable]
    public class MeshData
    {
        public string UUID { get; set; }
        public float Scale { get; set; }
        public MeshInfo Mesh { get; set; }
        public int[] Triangles { get; set; }
    }

    [System.Serializable]
    public class MeshInfo
    {
        public Vertices Vertices { get; set; }
        public float[] R { get; set; }
        public float[] G { get; set; }
        public float[] B { get; set; }
    }

    [System.Serializable]
    public class Vertices
    {
        public float[] X { get; set; }
        public float[] Y { get; set; }
        public float[] Z { get; set; }
    }

    private void Start()
    {
        // make default object invisible
        disableMeshDefaultObject();
        listening_text.enabled=false;
        
        //Permission Callbacks
        permissionCallbacks.OnPermissionGranted += OnPermissionGranted;
        permissionCallbacks.OnPermissionDenied += OnPermissionDenied;
        permissionCallbacks.OnPermissionDeniedAndDontAskAgain += OnPermissionDenied;
        
        // Requests permissions from the user. 
        MLPermissions.RequestPermission(MLPermission.VoiceInput, permissionCallbacks);
    }

    private void OnPermissionDenied(string permission)
    {
        Debug.Log("Permission Denied!");
    }

    private void OnPermissionGranted(string permission)
    {
        Initialize();
    }

    private void Initialize()
    {  
        if (MLVoice.VoiceEnabled)
        {
            MLResult result = MLVoice.SetupVoiceIntents(_voiceConfiguration);
            if (result.IsOk)
            {
                MLVoice.OnVoiceEvent += VoiceEvent;
            }
        }
    }

    void VoiceEvent(in bool wasSuccessful, in MLVoice.IntentEvent voiceEvent)
    {
        Debug.Log("start recording");
        yes.Play();
        listening_text.enabled = true;
        clip = Microphone.Start(null, false, 10, 16000);
        StartCoroutine(StopRecording());
    }

    private void OnDestroy()
    {
        MLVoice.Stop();
        MLVoice.OnVoiceEvent -= VoiceEvent;
        permissionCallbacks.OnPermissionGranted -= OnPermissionGranted;
        permissionCallbacks.OnPermissionDenied -= OnPermissionDenied;
        permissionCallbacks.OnPermissionDeniedAndDontAskAgain -= OnPermissionDenied;
    }

    private IEnumerator StopRecording()
    {
        yield return new WaitForSeconds(8);
        var position = Microphone.GetPosition(null);
        Microphone.End(null);
        listening_text.enabled = false;
        Debug.Log("Stop recording");
        end_listening.Play();
        var samples = new float[position * clip.channels];
        clip.GetData(samples, 0);
        bytes = EncodeAsWAV(samples, clip.frequency, clip.channels);
        StartCoroutine(SendAudioAndProcessResponse(bytes));
    }

    private byte[] EncodeAsWAV(float[] samples, int frequency, int channels)
    {
        using (var memoryStream = new MemoryStream(44 + samples.Length * 2))
        {
            using (var writer = new BinaryWriter(memoryStream))
            {
                writer.Write("RIFF".ToCharArray());
                writer.Write(36 + samples.Length * 2);
                writer.Write("WAVE".ToCharArray());
                writer.Write("fmt ".ToCharArray());
                writer.Write(16);
                writer.Write((ushort)1);
                writer.Write((ushort)channels);
                writer.Write(frequency);
                writer.Write(frequency * channels * 2);
                writer.Write((ushort)(channels * 2));
                writer.Write((ushort)16);
                writer.Write("data".ToCharArray());
                writer.Write(samples.Length * 2);

                foreach (var sample in samples)
                {
                    writer.Write((short)(sample * short.MaxValue));
                }
            }
            return memoryStream.ToArray();
        }
    }

    private IEnumerator SendAudioAndProcessResponse(byte[] audioData) {
        UnityWebRequest www = UnityWebRequest.PostWwwForm(fastApiUrl, "POST");
        
        WWWForm form = new WWWForm();
        form.AddBinaryData("audio", audioData, "recording.wav", "audio/wav");
        www.uploadHandler = new UploadHandlerRaw(form.data);
        www.downloadHandler = new DownloadHandlerBuffer();
        foreach (var header in form.headers)
        {
            www.SetRequestHeader(header.Key, header.Value);
        }

        Debug.Log("sending web request");
        activate_surprise_box=true;
        request_received.Play();
        yield return www.SendWebRequest();
        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("API Error: " + www.error);
        }
        else
        {
            string jsonResponse = www.downloadHandler.text;
            List<MeshData> meshDataList = JsonConvert.DeserializeObject<List<MeshData>>(jsonResponse);

            // MeshData firstResponse = meshDataList.Count > 0 ? meshDataList[0] : null;
            foreach (MeshData meshData in meshDataList)
            {
                float Scale = meshData.Scale;
                Debug.Log("received scal value is");
                Debug.Log(Scale);
                Mesh response_mesh = createMeshfromResponse(meshData);
                createGameObjfromResponseMesh(response_mesh, Scale);
            }
        }
        activate_surprise_box=false;
    }

    private Mesh createMeshfromResponse(MeshData response_) {
        Debug.Log("creating mesh from response");

        // Extract vertices
        Vector3[] _vertices = new Vector3[response_.Mesh.Vertices.X.Length];
        Color[] _vertex_colors = new Color[response_.Mesh.R.Length];
        for (int i = 0; i < _vertices.Length; i++)
        {
            _vertices[i] = new Vector3(response_.Mesh.Vertices.X[i], response_.Mesh.Vertices.Y[i], response_.Mesh.Vertices.Z[i]);
            _vertex_colors[i] = new Color(response_.Mesh.R[i], response_.Mesh.G[i], response_.Mesh.B[i], 1f);
        }

        // Create a new mesh and update it's vertices and triangles
        Mesh response_mesh = new Mesh();
        response_mesh.vertices = _vertices;

        response_mesh.triangles = response_.Triangles;
        
        // update colors for each vertex in mesh
        response_mesh.colors = _vertex_colors;
    
        // recalculate normals
        response_mesh.RecalculateNormals();
        
        return response_mesh;
    }

    void createGameObjfromResponseMesh(Mesh response_mesh, float Scale) {
        Debug.Log("creating new game object now");
        copiedObject = defaultGameObject;
        GameObject responseGameObject =  Instantiate(copiedObject);
        responseGameObject.GetComponent<MeshFilter>().mesh = response_mesh;
        // responseGameObject.transform.SetParent(this.transform);
        Vector3 position_camera = main_camera.transform.position;


        // get scale
        Vector3 localScale = responseGameObject.transform.localScale;
        Debug.Log(localScale);
        float new_scale = (localScale.y)*Scale*Scale;
        // update scale with a scaling factor received from response
        // responseGameObject.transform.localScale = new Vector3(localScale.x, new_scale, localScale.z);
        responseGameObject.transform.localScale = new Vector3(new_scale, new_scale, new_scale);
        Debug.Log(responseGameObject.transform.localScale);


        // Calculate the final position by adding the offset to the parent's position
        Vector3 positionOffset = new Vector3(0.0f, 0.0f, 1f);
        // Vector3 finalPosition = this.transform.position + positionOffset;
        Vector3 finalPosition = position_camera + positionOffset;

        // Set the position of loadedObj relative to the parent's position
        responseGameObject.transform.position = finalPosition;
        ready_sound.Play();
    }

    void disableMeshDefaultObject() {
        MeshFilter filter = defaultGameObject.GetComponent<MeshFilter>();
        Mesh mesh = new Mesh();
        mesh.vertices = new Vector3[]
        {
            new Vector3(0, 0, 0),
            new Vector3(0.0000001f, 0, 0),
            new Vector3(0.0000001f, 0.0000001f, 0),
        };

        mesh.triangles = new int[] {0, 2, 1};

        Color[] colors = new Color[mesh.vertices.Length];
        for (int i = 0; i < colors.Length; i++)
        {
            colors[i] = new Color(0f, 0f, 0f, 0f); // Set alpha to 0
        }
        mesh.colors = colors;

        if (filter != null)
        {
            filter.mesh = mesh;
        }
    }

    void Update()
    {
        if (activate_surprise_box) {
            mario_box.SetActive(true);
            Vector3 position_camera = main_camera.transform.position;
            Vector3 positionOffset = new Vector3(0.0f, 0.0f, 1f);
            Vector3 finalPosition = position_camera + positionOffset;
            mario_box.transform.position = finalPosition;

            // rotate mario box
            mario_box.transform.Rotate(Vector3.up, mario_rotation_speed * Time.deltaTime);
        }
        else {
            mario_box.SetActive(false);
        }
    }
}

