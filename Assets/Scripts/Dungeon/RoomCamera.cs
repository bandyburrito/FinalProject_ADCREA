using UnityEngine;
using Unity.Cinemachine;
using ADCREA.Algorithms;

namespace ADCREA.Dungeon
{
    /// <summary>
    /// Keeps the camera locked onto whichever room is active, Isaac-style: one room
    /// fills the screen, and entering a door slides the view over to the next room.
    ///
    /// Drives the scene's existing Cinemachine rig when one is present - instead of
    /// following the player, the virtual camera follows an invisible anchor that this
    /// script parks at the active room's centre, so Cinemachine's damping provides the
    /// room-to-room glide for free. Without Cinemachine it hard-snaps the raw camera.
    /// </summary>
    public class RoomCamera : MonoBehaviour
    {
        [Tooltip("Half the vertical world size shown. Sized so one room fills the screen, Isaac-style.")]
        public float orthographicSize = 9.75f;

        [Tooltip("Shifts the view up from the room centre: the art's bottom margin is thicker than its top, so a small lift balances the framing.")]
        public float verticalOffset = 1.25f;

        private Transform _anchor;
        private CinemachineCamera _virtualCamera;
        private Camera _sceneCamera;
        private bool _subscribed;
        private bool _firstFocus = true;

        private void OnEnable()
        {
            TrySubscribe();
        }

        private void Start()
        {
            // RoomManager might not have existed yet during OnEnable, so try again once
            // every scene object has finished Awake.
            TrySubscribe();
        }

        private void OnDisable()
        {
            if (_subscribed && RoomManager.Instance != null)
            {
                RoomManager.Instance.ActiveRoomChanged -= HandleActiveRoomChanged;
                _subscribed = false;
            }
        }

        private void TrySubscribe()
        {
            if (_subscribed || RoomManager.Instance == null)
            {
                return;
            }
            RoomManager.Instance.ActiveRoomChanged += HandleActiveRoomChanged;
            _subscribed = true;
        }

        private void HandleActiveRoomChanged(RoomGrid room)
        {
            FocusRoom(room);
        }

        public void FocusRoom(RoomGrid room)
        {
            if (room == null)
            {
                return;
            }

            Vector3 center = room.transform.position + new Vector3(
                room.width * room.cellSize * 0.5f,
                room.height * room.cellSize * 0.5f + verticalOffset,
                0f);

            EnsureCameraReferences();

            if (_virtualCamera != null)
            {
                FocusWithCinemachine(center);
            }
            else if (_sceneCamera != null)
            {
                _sceneCamera.orthographicSize = orthographicSize;
                _sceneCamera.transform.position = center + new Vector3(0f, 0f, -10f);
            }
        }

        private void FocusWithCinemachine(Vector3 center)
        {
            if (_anchor == null)
            {
                _anchor = new GameObject("RoomCameraAnchor").transform;
            }
            _anchor.position = center;
            _virtualCamera.Follow = _anchor;

            LensSettings lens = _virtualCamera.Lens;
            lens.OrthographicSize = orthographicSize;
            _virtualCamera.Lens = lens;

            // The rotation composer is a 3D aiming behaviour; on a 2D orthographic camera
            // it would tilt the view while the position damps towards a new room.
            CinemachineRotationComposer rotationComposer = _virtualCamera.GetComponent<CinemachineRotationComposer>();
            if (rotationComposer != null && rotationComposer.enabled)
            {
                rotationComposer.enabled = false;
            }

            if (_firstFocus)
            {
                // Without this the camera would glide all the way from the editor's old
                // viewpoint to the generated dungeon on the very first frame.
                Vector3 offset = new Vector3(0f, 0f, -10f);
                CinemachineFollow follow = _virtualCamera.GetComponent<CinemachineFollow>();
                if (follow != null)
                {
                    offset = follow.FollowOffset;
                }
                _virtualCamera.ForceCameraPosition(center + offset, Quaternion.identity);
                _firstFocus = false;
            }
        }

        private void EnsureCameraReferences()
        {
            if (_virtualCamera == null)
            {
                _virtualCamera = FindAnyObjectByType<CinemachineCamera>();
            }
            if (_sceneCamera == null)
            {
                _sceneCamera = FindAnyObjectByType<Camera>();
            }
            if (_sceneCamera != null)
            {
                // Rooms are spaced-out islands; a near-black clear colour makes the gaps
                // between them read as intentional void instead of a default blue screen.
                _sceneCamera.clearFlags = CameraClearFlags.SolidColor;
                _sceneCamera.backgroundColor = new Color(0.04f, 0.04f, 0.06f);
            }
        }
    }
}
