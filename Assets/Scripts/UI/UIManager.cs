using TMPro;
using UnityEngine;
using ChiecLopCuoi.Database;

/// <summary>Displays and persists the player's tire and population balances.</summary>
public sealed class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Player Resources")]
    [Min(0)] public int currentTires = 200;
    [Min(0)] public int currentPopulation;
    [Min(1)] public int maxPopulation = 20;

    [Header("HUD References")]
    public TMP_Text tiresText;
    public TMP_Text populationText;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        GameRepository.LoadPlayerData(out currentTires, out currentPopulation);
        RefreshDisplay();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void AddTires(int amount)
    {
        if (amount <= 0) return;
        currentTires += amount;
        SaveAndRefresh();
    }

    public bool SpendTires(int amount)
    {
        if (amount < 0 || currentTires < amount) return false;
        currentTires -= amount;
        SaveAndRefresh();
        return true;
    }

    public void UpdatePopulation(int current, int max)
    {
        currentPopulation = Mathf.Max(0, current);
        maxPopulation = Mathf.Max(1, max);
        SaveAndRefresh();
    }

    private void SaveAndRefresh()
    {
        GameRepository.SavePlayerData(currentTires, currentPopulation);
        RefreshDisplay();
    }

    private void RefreshDisplay()
    {
        if (tiresText != null) tiresText.text = currentTires.ToString();
        if (populationText != null) populationText.text = $"{currentPopulation}/{maxPopulation}";
    }
}
