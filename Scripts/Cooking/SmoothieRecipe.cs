using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Recipe", menuName = "Game/SmoothieRecipe")]
public class SmoothieRecipe : ScriptableObject
{
    [Header("조합")]
    [SerializeField] private IngredientType[] requiredIngredients;

    [Header("결과")]
    [SerializeField] private SmoothieType result;
    [SerializeField] private float blendDuration = 2f;

    [Header("표시")]
    [SerializeField] private Color smoothieColor = Color.white;
    [SerializeField] private Sprite icon;

    // 외부에서 읽기용 프로퍼티 (캡슐화 유지)
    public SmoothieType Result => result;
    public float BlendDuration => blendDuration;
    public Color SmoothieColor => smoothieColor;
    public Sprite Icon => icon;

    /// <summary>
    /// 투입된 재료가 이 레시피와 일치하는지 (순서 무관)
    /// </summary>
    public bool Matches(List<IngredientType> inserted)
    {
        // 개수 다르면 바로 불일치
        if (inserted.Count != requiredIngredients.Length)
            return false;

        // 재료별 개수를 세서 비교 (순서 무관 + 중복 재료도 정확히)
        var required = CountIngredients(requiredIngredients);
        var actual = CountIngredients(inserted);

        foreach (var pair in required)
        {
            if (!actual.TryGetValue(pair.Key, out int count) || count != pair.Value)
                return false;
        }
        return true;
    }

    // 재료 → 개수 딕셔너리
    private Dictionary<IngredientType, int> CountIngredients(IEnumerable<IngredientType> list)
    {
        var dict = new Dictionary<IngredientType, int>();
        foreach (var ing in list)
        {
            if (dict.ContainsKey(ing)) dict[ing]++;
            else dict[ing] = 1;
        }
        return dict;
    }
}