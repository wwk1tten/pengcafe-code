using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class PenguinInteractor : MonoBehaviour
{
    public event Action<Transform> OnInteractableTargetChanged;
    [SerializeField] private PenguinHands hands;
    [SerializeField] private PlayerInput playerInput;
    [SerializeField] private DroppedItem droppedItemPrefab;
    [SerializeField] private HeldPropDisplay heldPropDisplay;
    [SerializeField] private int dropSoundIndex = 6;
    [SerializeField] private int clearBlenderSoundIndex = 6;

    private IInteractable _currentTarget;
    private InteractableHighlight _currentHighlight;
    private InputAction _interactAction;
    private InputAction _dropAction;
    private InputAction _clearBlenderAction;
    private DroppedItem _currentDropped;

    private void Awake()
    {
        if (hands == null) hands = GetComponent<PenguinHands>();
        if (playerInput == null) playerInput = GetComponent<PlayerInput>();
        _interactAction = playerInput.actions.FindAction("Interact");
        _dropAction = playerInput.actions.FindAction("Drop");
        _clearBlenderAction = playerInput.actions.FindAction("ClearBlender");
    }

    private void OnEnable()
    {
        _interactAction?.Enable();
        _dropAction?.Enable();
        _clearBlenderAction?.Enable();
    }

    private void OnDisable()
    {
        _interactAction?.Disable();
        _dropAction?.Disable();
        _clearBlenderAction?.Disable();
    }

    private void Update()
    {
        if (_interactAction != null && _interactAction.WasPressedThisFrame())
            _currentTarget?.Interact(hands);

        if (_dropAction != null && _dropAction.WasPressedThisFrame())
            TryDrop();

        if (_clearBlenderAction != null && _clearBlenderAction.WasPressedThisFrame())
            TryClearBlender();
    }

    private void TryDrop()
    {
        if (hands.IsEmpty) return;

        if (_currentDropped != null)
            Destroy(_currentDropped.gameObject);

        GameObject prop = heldPropDisplay != null ? heldPropDisplay.ReleaseCurrentProp() : null;

        if (prop != null)
        {
            var col = prop.AddComponent<SphereCollider>();
            col.isTrigger = true;
            col.radius = 0.4f;
            _currentDropped = prop.AddComponent<DroppedItem>();
        }
        else
        {
            if (droppedItemPrefab == null) { hands.Clear(); return; }
            _currentDropped = Instantiate(droppedItemPrefab, transform.position, Quaternion.identity);
        }

        _currentDropped.Init(hands);
        hands.Clear();
        SoundManager.Instance?.PlayEffect(dropSoundIndex);
    }

    private void TryClearBlender()
    {
        if (_currentTarget is Blender blender)
        {
            blender.Clear();
            SoundManager.Instance?.PlayEffect(clearBlenderSoundIndex);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent<IInteractable>(out var interactable))
        {
            _currentTarget = interactable;
            _currentHighlight?.SetHighlight(false);
            _currentHighlight = other.GetComponent<InteractableHighlight>();
            _currentHighlight?.SetHighlight(true);
            OnInteractableTargetChanged?.Invoke(other.transform);
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (_currentTarget != null) return;
        if (other.TryGetComponent<IInteractable>(out var interactable))
        {
            _currentTarget = interactable;
            _currentHighlight = other.GetComponent<InteractableHighlight>();
            _currentHighlight?.SetHighlight(true);
            OnInteractableTargetChanged?.Invoke(other.transform);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent<IInteractable>(out var interactable))
        {
            if (_currentTarget == (object)interactable)
            {
                _currentHighlight?.SetHighlight(false);
                _currentHighlight = null;
                _currentTarget = null;
                OnInteractableTargetChanged?.Invoke(null);
            }
        }
    }
}