using UnityEngine;
using Unity.Cinemachine;
using ADCREA.Algorithms;

namespace ADCREA.Dungeon
{

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

            CinemachineRotationComposer rotationComposer = _virtualCamera.GetComponent<CinemachineRotationComposer>();
            if (rotationComposer != null && rotationComposer.enabled)
            {
                rotationComposer.enabled = false;
            }

            if (_firstFocus)
            {

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

                _sceneCamera.clearFlags = CameraClearFlags.SolidColor;
                _sceneCamera.backgroundColor = new Color(0.04f, 0.04f, 0.06f);
            }
        }
    }
}
