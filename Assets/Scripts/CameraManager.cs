using UnityEngine;

public class CameraManager : MonoBehaviour
{
    [Header("Focus Object")]
    [SerializeField, Tooltip("Enable double-click to focus on objects?")]
    private bool doFocus = false;

    [SerializeField] private float focusLimit = 100f;
    [SerializeField] private float minFocusDistance = 5f;
    private float doubleClickTime = .15f;
    private float cooldown = 0;

    [Header("Undo - Only undoes the Focus Object - The keys must be pressed in order.")]
    [SerializeField] private KeyCode firstUndoKey = KeyCode.LeftControl;

    [SerializeField] private KeyCode secondUndoKey = KeyCode.Z;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 1f;

    [SerializeField] private float rotationSpeed = 10f;
    [SerializeField] private float zoomSpeed = 10f;
    //[SerializeField] private Vector3 motionRange = new Vector3(30, 30, 30);
    //[SerializeField] private float smoothing = 10f;

    //Cache last pos and rot be able to undo last focus object action.
    private Quaternion prevRot = new Quaternion();

    private Vector3 prevPos = new Vector3();
    private Quaternion startRot = new Quaternion();
    private Vector3 startPos = new Vector3();

    [Header("Axes Names")]
    [SerializeField, Tooltip("Otherwise known as the vertical axis")] private string mouseY = "Mouse Y";

    [SerializeField, Tooltip("AKA horizontal axis")] private string mouseX = "Mouse X";
    [SerializeField, Tooltip("The axis you want to use for zoom.")] private string zoomAxis = "Mouse ScrollWheel";

    [Header("Move Keys")]
    [SerializeField] private KeyCode forwardKey = KeyCode.W;

    [SerializeField] private KeyCode backKey = KeyCode.S;
    [SerializeField] private KeyCode leftKey = KeyCode.A;
    [SerializeField] private KeyCode rightKey = KeyCode.D;

    [Header("Flat Move"), Tooltip("Instead of going where the camera is pointed, the camera moves only on the horizontal plane (Assuming you are working in 3D with default preferences).")]
    [SerializeField] private KeyCode flatMoveKey = KeyCode.LeftShift;

    [Header("Anchored Movement"), Tooltip("By default in scene-view, this is done by right-clicking for rotation or middle mouse clicking for up and down")]
    [SerializeField] private KeyCode anchoredMoveKey = KeyCode.Mouse2;

    [SerializeField] private KeyCode anchoredRotateKey = KeyCode.Mouse1;

    private Vector3 targetPosition;

    private void Start()
    {
        startRot = transform.rotation;
        startPos = transform.position;
        SavePosAndRot();
    }

    private void Update()
    {
        if (!doFocus)
            return;

        //Double click for focus
        if (cooldown > 0 && Input.GetKeyDown(KeyCode.Mouse0))
            FocusObject();
        if (Input.GetKeyDown(KeyCode.Mouse0))
            cooldown = doubleClickTime;

        //--------UNDO FOCUS---------
        if (Input.GetKey(firstUndoKey))
        {
            if (Input.GetKeyDown(secondUndoKey))
                GoBackToLastPosition();
        }

        cooldown -= Time.deltaTime;
    }

    private void LateUpdate()
    {
        Vector3 move = Vector3.zero;

        //Move and rotate the camera

        if (Input.GetKey(forwardKey))
            move += Vector3.forward * moveSpeed;
        if (Input.GetKey(backKey))
            move += Vector3.back * moveSpeed;
        if (Input.GetKey(leftKey))
            move += Vector3.left * moveSpeed;
        if (Input.GetKey(rightKey))
            move += Vector3.right * moveSpeed;

        //By far the simplest solution I could come up with for moving only on the Horizontal plane - no rotation, just cache y
        if (Input.GetKey(flatMoveKey))
        {
            float origY = transform.position.y;

            transform.Translate(move);
            transform.position = new Vector3(transform.position.x, origY, transform.position.z);

            return;
        }

        float mouseMoveY = Input.GetAxis(mouseY);
        float mouseMoveX = Input.GetAxis(mouseX);

        //Move the camera when anchored
        if (Input.GetKey(anchoredMoveKey))
        {
            move += Vector3.up * mouseMoveY * -moveSpeed;
            move += Vector3.right * mouseMoveX * -moveSpeed;
        }

        //Rotate the camera when anchored
        if (Input.GetKey(anchoredRotateKey))
        {
            transform.RotateAround(transform.position, transform.right, mouseMoveY * -rotationSpeed);
            transform.RotateAround(transform.position, Vector3.up, mouseMoveX * rotationSpeed);
        }

        //Vector3 nextTargetPosition = transform.position + move;
        //if (IsInBounds(nextTargetPosition)) targetPosition = nextTargetPosition;
        //transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * smoothing);
        transform.Translate(move);

        //Scroll to zoom
        float mouseScroll = Input.GetAxis(zoomAxis);
        transform.Translate(Vector3.forward * mouseScroll * zoomSpeed);
    }

    private void FocusObject()
    {
        //To be able to undo
        SavePosAndRot();

        //If we double-clicked an object in the scene, go to its position
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, focusLimit))
        {
            GameObject target = hit.collider.gameObject;
            Vector3 targetPos = target.transform.position;
            Vector3 targetSize = hit.collider.bounds.size;

            transform.position = targetPos + GetOffset(targetPos, targetSize);

            transform.LookAt(target.transform);
        }
    }

    private void SavePosAndRot()
    {
        prevRot = transform.rotation;
        prevPos = transform.position;
    }

    private void GoBackToLastPosition()
    {
        transform.position = prevPos;
        transform.rotation = prevRot;
    }

    private Vector3 GetOffset(Vector3 targetPos, Vector3 targetSize)
    {
        Vector3 dirToTarget = targetPos - transform.position;

        float focusDistance = Mathf.Max(targetSize.x, targetSize.z);
        focusDistance = Mathf.Clamp(focusDistance, minFocusDistance, focusDistance);

        return -dirToTarget.normalized * focusDistance;
    }

    //[Header("Camera Motion Setting")]
    //[SerializeField] private bool isCameraMotion = true;
    //[SerializeField] private float motionSpeed = .25f;
    //[SerializeField] private float motionSmoothing = 10f;
    //[SerializeField] private Vector2 motionRange = new Vector2(30, 30);

    //private Vector3 input;

    //[Header("Camera Rotation Setting")]
    //[SerializeField] private bool isCameraRotation = true;
    //[SerializeField] private float rotationSpeed = .25f;
    //[SerializeField] private float rotationSmoothing = 10f;
    //private float targetAngle;
    //private float currentAngle;

    //private void Awake()
    //{
    //    //motion
    //    targetPosition = transform.position;

    //    //rotation
    //    targetAngle = transform.eulerAngles.y;
    //    currentAngle = targetAngle;
    //}

    //private void Update()
    //{
    //    HandleInput();
    //    if(isCameraMotion) Move();
    //    if(isCameraRotation) Rotate();
    //}
    //private void HandleInput()
    //{
    //    float x = Input.GetAxisRaw("Horizontal");
    //    float z = Input.GetAxisRaw("Vertical");

    //    Vector3 right = transform.right * x;
    //    Vector3 forward = transform.localPosition * z;

    //    input = (forward + right).normalized;

    //    if (!Input.GetMouseButton(1)) return;
    //    targetAngle += Input.GetAxisRaw("Mouse ScrollWheel") * rotationSpeed;
    //}

    //private void Move()
    //{
    //    Vector3 nextTargetPosition = targetPosition + input * motionSpeed;
    //    if(IsInBounds(nextTargetPosition)) targetPosition = nextTargetPosition;
    //    transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * motionSmoothing);
    //}

    //private void Rotate()
    //{
    //    currentAngle = Mathf.Lerp(currentAngle, targetAngle, Time.deltaTime * rotationSmoothing);
    //    transform.rotation = Quaternion.AngleAxis(currentAngle, Vector3.up);
    //}

    //private bool IsInBounds(Vector3 pos)
    //{
    //    return pos.x > -motionRange.x &&
    //        pos.x < motionRange.x &&
    //        pos.y > -motionRange.y &&
    //        pos.y < motionRange.y &&
    //        pos.z > -motionRange.z &&
    //        pos.z < motionRange.z;
    //}

    //private void OnDrawGizmos()
    //{
    //    Gizmos.color = Color.red;
    //    //Gizmos.DrawSphere(transform.position, 5f);
    //    Gizmos.DrawWireCube(Vector3.zero, new Vector3(motionRange.x * 2f, motionRange.y * 2f, motionRange.z * 2f));
    //}
}