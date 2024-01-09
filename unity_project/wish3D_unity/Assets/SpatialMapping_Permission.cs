using UnityEngine;
using UnityEngine.XR.MagicLeap;

public class Meshing : MonoBehaviour
{
    
    [SerializeField] MeshingSubsystemComponent meshingSubsystemComponent;
    private readonly MLPermissions.Callbacks mlPermissionsCallbacks = new MLPermissions.Callbacks();
   

    private void Awake()
    {
        // subscribe to permission events
        mlPermissionsCallbacks.OnPermissionGranted += MlPermissionsCallbacks_OnPermissionGranted;
        mlPermissionsCallbacks.OnPermissionDenied += MlPermissionsCallbacks_OnPermissionDenied;
        mlPermissionsCallbacks.OnPermissionDeniedAndDontAskAgain += MlPermissionsCallbacks_OnPermissionDenied;
        
    }

    void Start()
    {
        // request permission at start
        MLPermissions.RequestPermission(MLPermission.SpatialMapping, mlPermissionsCallbacks);

        // get meshing subsystem
        meshingSubsystemComponent = FindObjectOfType<MeshingSubsystemComponent>();
    }


    // if permission denied, disable meshing subsystem
    private void MlPermissionsCallbacks_OnPermissionDenied(string permission)
    {
        meshingSubsystemComponent.enabled = false;
    }

    // if permission granted, enable meshing subsystem
    private void MlPermissionsCallbacks_OnPermissionGranted(string permission)
    {
        meshingSubsystemComponent.enabled = true;

    }

    private void OnDestroy()
    {
        // unsubscribe from permission events
        mlPermissionsCallbacks.OnPermissionGranted -= MlPermissionsCallbacks_OnPermissionGranted;
        mlPermissionsCallbacks.OnPermissionDenied -= MlPermissionsCallbacks_OnPermissionDenied;
        mlPermissionsCallbacks.OnPermissionDeniedAndDontAskAgain -= MlPermissionsCallbacks_OnPermissionDenied;
    }

}