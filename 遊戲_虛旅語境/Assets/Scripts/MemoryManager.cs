using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

/// <summary>
/// 泛型記憶類別，支援角色、類型、時間與權重
/// </summary>
[Serializable]
public class Memory<T>
{
    public string id;          // 唯一識別碼
    public string role;        // "player" 或 "ai" 或其他
    public T content;          // 記憶內容
    public string type;        // info / event / emotion 等
    public float weight;       // 重要性權重
    public DateTime timestamp; // 記憶時間

    public Memory(string role, T content, string type = "info", float weight = 1f)
    {
        this.id = Guid.NewGuid().ToString();
        this.role = role;
        this.content = content;
        this.type = type;
        this.weight = weight;
        this.timestamp = DateTime.Now;
    }
}

/// <summary>
/// 記憶管理器，單例模式
/// </summary>
public class MemoryManager : MonoBehaviour
{
    public static MemoryManager Instance;

    // 泛型使用 string 為主（對話文字）
    private List<Memory<string>> memories = new List<Memory<string>>();

    private string saveFilePath;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            saveFilePath = Path.Combine(Application.persistentDataPath, "memories.json");
            LoadMemories();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    #region 記憶操作

    /// <summary>
    /// 新增記憶
    /// </summary>
    public void AddMemory(string role, string content, string type = "info", float weight = 1f)
    {
        if (string.IsNullOrEmpty(content)) return;

        Memory<string> m = new Memory<string>(role, content, type, weight);
        memories.Add(m);
        SaveMemories();
    }

    /// <summary>
    /// 刪除指定 ID 的記憶
    /// </summary>
    public bool RemoveMemory(string id)
    {
        var mem = memories.Find(x => x.id == id);
        if (mem != null)
        {
            memories.Remove(mem);
            SaveMemories();
            return true;
        }
        return false;
    }

    /// <summary>
    /// 取得所有記憶，可依角色過濾
    /// </summary>
    public List<Memory<string>> GetAllMemories(string role = null, bool sortByWeight = false)
    {
        IEnumerable<Memory<string>> result = memories;

        if (!string.IsNullOrEmpty(role))
            result = result.Where(m => m.role.Equals(role, StringComparison.OrdinalIgnoreCase));

        return sortByWeight ? result.OrderByDescending(m => m.weight).ToList() : result.OrderBy(m => m.timestamp).ToList();
    }

    /// <summary>
    /// 搜尋包含關鍵字的記憶，可指定角色
    /// </summary>
    public List<Memory<string>> SearchMemory(string keyword, string role = null)
    {
        IEnumerable<Memory<string>> result = memories;

        if (!string.IsNullOrEmpty(role))
            result = result.Where(m => m.role.Equals(role, StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrEmpty(keyword))
            result = result.Where(m => m.content.Contains(keyword, StringComparison.OrdinalIgnoreCase));

        return result.OrderByDescending(m => m.timestamp).ToList();
    }

    #endregion

    #region JSON 存檔與讀檔

    private void SaveMemories()
    {
        try
        {
            string json = JsonUtility.ToJson(new MemoryListWrapper { items = memories }, true);
            File.WriteAllText(saveFilePath, json);
        }
        catch (Exception e)
        {
            Debug.LogError($"[MemoryManager] Save Error: {e.Message}");
        }
    }

    private void LoadMemories()
    {
        try
        {
            if (File.Exists(saveFilePath))
            {
                string json = File.ReadAllText(saveFilePath);
                MemoryListWrapper wrapper = JsonUtility.FromJson<MemoryListWrapper>(json);
                memories = wrapper.items ?? new List<Memory<string>>();
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[MemoryManager] Load Error: {e.Message}");
            memories = new List<Memory<string>>();
        }
    }

    [Serializable]
    private class MemoryListWrapper
    {
        public List<Memory<string>> items = new List<Memory<string>>();
    }

    #endregion

    #region OpenAIManager 整合

    /// <summary>
    /// 搜尋相關記憶，生成文字列表，可直接注入 prompt
    /// </summary>
    public string GetRelevantMemoriesText(string keyword, string role = "player", int maxCount = 5)
    {
        var relevant = SearchMemory(keyword, role)
            .Take(maxCount)
            .Select(m => $"({m.timestamp:MM-dd HH:mm}) [{m.type}] {m.content}");

        return string.Join("\n", relevant);
    }

    #endregion
}