using System;
using System.Collections.Generic;
using UnityEngine;

public class Blender : MonoBehaviour, IInteractable
{
    public static Blender Instance { get; private set; }

    [Header("References")]
    [SerializeField] private SmoothieDatabase database;

    [Header("Settings")]
    [SerializeField] private bool autoAddIce = true;        // 첫 재료 시 얼음 자동
    [SerializeField] private float defaultBlendTime = 2f;   // 실패작(레시피 없음) 갈기 시간
    // 외부 알림 (UI/사운드용)
    public event Action<BlenderState> OnStateChanged;
    public event Action<IngredientType> OnIngredientAdded;
    public SmoothieType ResultType => _result;
    public BlenderState State { get; private set; } = BlenderState.Empty;
    private Color _currentLiquidColor = Color.clear;

    private readonly List<IngredientType> _ingredients = new List<IngredientType>();
    private SmoothieType _result = SmoothieType.None;
    private float _blendTimer;
    private float _blendDuration;
    

    // ---- 외부에서 호출되는 행동 ----

    // 재료 투입 (Empty/Filled에서만)
    public bool AddIngredient(IngredientType type)
    {
        if (State != BlenderState.Empty)
            return false;

        if (autoAddIce && _ingredients.Count == 0 && type != IngredientType.Ice)
        {
            _ingredients.Add(IngredientType.Ice);
            OnIngredientAdded?.Invoke(IngredientType.Ice);
        }

        _ingredients.Add(type);

        // 색 혼합
        Color newColor = GetIngredientColor(type);
        if (_currentLiquidColor == Color.clear)
            _currentLiquidColor = newColor;
        else
            _currentLiquidColor = Color.Lerp(_currentLiquidColor, newColor, 0.5f);

        ChangeState(BlenderState.Filled);
        OnLiquidColorChanged?.Invoke(_currentLiquidColor);  // 이벤트
        OnIngredientAdded?.Invoke(type);
        return true;
    }

    // 갈기 시작 (Filled에서만)
    public bool StartBlending()
    {
        if (State != BlenderState.Filled) return false;
        FinishBlending();   // 결과 미리 확정 (비주얼이 색 읽어야 하므로)
        _blendTimer = _blendDuration;
        ChangeState(BlenderState.Blending);
        return true;
    }
    
    // 완성품 픽업 (Done에서만) → 결과 반환 + 비움
    public SmoothieType PickUpSmoothie()
    {
        if (State != BlenderState.Done) return SmoothieType.None;

        SoundManager.Instance.PlayEffect(0); 
        SmoothieType picked = _result;
        ResetBlender();
        return picked;
    }

    // 비우기 (쓰레기통 / 잘못 넣었을 때)
    public void Clear()
    {
        ResetBlender();
    }

    
    public void Interact(PenguinHands hands)
    {
        // 경우 1: 재료 들고 있음 → 블렌더에 투입
        if (hands.Content == PenguinHands.HandContent.Ingredient)
        {
            if (AddIngredient(hands.HeldIngredient))
            {
                hands.Clear();
            }
            else
                Debug.Log("지금 재료 못 넣음");
            return;
        }

        // 경우 2: 빈손 + 블렌더 완성됨 → 픽업
        if (hands.IsEmpty && State == BlenderState.Done)
        {
            SmoothieType s = PickUpSmoothie();
            hands.HoldSmoothie(s);
            return;
        }

        // 경우 3: 빈손 + Filled → 갈기 시작
        if (hands.IsEmpty && State == BlenderState.Filled)
        {
            StartBlending();
            return;
        }

    }

    public float BlendProgress
    {
        get
        {
            if (State != BlenderState.Blending) return 0f;
            if (_blendDuration <= 0f) return 1f;
            return 1f - (_blendTimer / _blendDuration);
            // 0(시작) → 1(완성)
        }
    }
    // ---- 내부 ----

    private void Awake() { Instance = this; }

    private void Update()
    {
        if (State != BlenderState.Blending) return;

        _blendTimer -= Time.deltaTime;
        if (_blendTimer <= 0f)
            ChangeState(BlenderState.Done);
    }


    private void ResetBlender()
    {
        _ingredients.Clear();
        _result = SmoothieType.None;
        _blendTimer = 0f;
        _currentLiquidColor = Color.clear;
        OnLiquidColorChanged?.Invoke(Color.clear);
        ChangeState(BlenderState.Empty);
    }

    private void ChangeState(BlenderState next)
    {
        if (State == next) return;
        State = next;
        OnStateChanged?.Invoke(next);
    }
    private void FinishBlending()
    {
        SmoothieRecipe recipe = database.FindRecipe(_ingredients);
        if (recipe != null)
        {
            _result = recipe.Result;
            _blendDuration = recipe.BlendDuration;
        }
        else
        {
            _result = SmoothieType.None;
            _blendDuration = defaultBlendTime;
        }
    }
    

    // 이벤트
    public event Action<Color> OnLiquidColorChanged;

    private Color GetIngredientColor(IngredientType type)
    {
        switch (type)
        {
            case IngredientType.Ice:            return new Color(0.8f, 0.95f, 1f);   // 연한 파랑
            case IngredientType.Watermelon:     return Color.red;
            case IngredientType.Avocado:        return new Color(0.7f, 1f, 0.4f);
            case IngredientType.Pineapple:      return Color.yellow;
            case IngredientType.Coconut:        return new Color(0.95f, 0.92f, 0.81f);
            default: return Color.gray;
        }
    }

    // ---- 조회용 ----

    public bool IsDone => State == BlenderState.Done;
    public bool CanAddIngredient => State == BlenderState.Empty || State == BlenderState.Filled;
    public IReadOnlyList<IngredientType> CurrentIngredients => _ingredients;
}