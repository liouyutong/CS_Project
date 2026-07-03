using UnityEngine;

public class ChatSceneLoader : MonoBehaviour
{
    public CharacterEquipmentHandler equipmentHandler;
    public FaceAutoGenerator faceGenerator;

    [Header("動畫設定")]
    public RuntimeAnimatorController chatController;

    [Header("UI 容器")]
    public GameObject mainFaceUIPanel;

    void Start()
    {
        if (CharacterDataManager.Instance != null)
        {
            var data = CharacterDataManager.Instance;

            // 1. UI 與臉部模式
            if (faceGenerator != null)
            {
                faceGenerator.isChatMode = true;
                if (mainFaceUIPanel != null) mainFaceUIPanel.SetActive(false);
                faceGenerator.contentParent?.parent?.parent?.gameObject.SetActive(false);
            }

            // 2. 角色顯隱與對接
            if (equipmentHandler != null && equipmentHandler.genderSwitcher != null)
            {
                bool isMale = data.selectedGender == 0;
                GameObject activeModel = isMale ? equipmentHandler.genderSwitcher.maleModel : equipmentHandler.genderSwitcher.femaleModel;

                equipmentHandler.genderSwitcher.maleModel.SetActive(isMale);
                equipmentHandler.genderSwitcher.femaleModel.SetActive(!isMale);

                // 【關鍵】更新當前模型記錄，讓 OpenAIManager 能抓到
                equipmentHandler.genderSwitcher.currentActiveModel = activeModel;

                // 3. 還原顏色與捏臉
                equipmentHandler.genderSwitcher.ApplyColors(activeModel, data.skinColor, data.eyeColor);
                if (faceGenerator != null)
                {
                    faceGenerator.targetMesh = activeModel.GetComponentInChildren<SkinnedMeshRenderer>();
                    foreach (var record in data.faceShapeData)
                    {
                        faceGenerator.UpdateMeshBlendShapeByName(record.Key, record.Value);
                    }
                }

                // 4. 強制切換動畫控制器
                Animator anim = activeModel.GetComponent<Animator>();
                if (anim == null) anim = activeModel.AddComponent<Animator>();
                anim.runtimeAnimatorController = chatController;
                anim.Rebind();

                Debug.Log("<color=green>ChatSceneLoader: 模型準備完畢，等待 AI 自動對接</color>");
            }
        }
    }
}