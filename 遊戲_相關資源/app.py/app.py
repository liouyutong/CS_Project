import os
import json
import datetime
import re
import numpy as np
import faiss
from flask import Flask, request, jsonify
from flask_cors import CORS
from sentence_transformers import SentenceTransformer
from openai import OpenAI
import re

# --- 1. 初始化設定 ---
# 請改用環境變數提供 API Key（避免把金鑰寫進程式碼 / 上傳到 GitHub）
# Windows（PowerShell）示例：
#   $env:OPENAI_API_KEY="你的金鑰"
MY_API_KEY = os.getenv("OPENAI_API_KEY")
client = OpenAI(api_key=MY_API_KEY)

app = Flask(__name__)
CORS(app)

MEMORY_FILE = "memory_db.json"
# 使用支援繁體中文的語義分析模型
embed_model = SentenceTransformer('paraphrase-multilingual-MiniLM-L12-v2')

memory_db = []
# FAISS 索引 (384 維度對應 MiniLM 模型)
index = faiss.IndexFlatL2(384)
chat_counter = 0  # 用於追蹤 MBTI 分析觸發時機
# --- 2. 記憶核心邏輯 ---

def save_to_disk():
    """將記憶庫保存到硬碟"""
    with open(MEMORY_FILE, 'w', encoding='utf-8') as f:
        json.dump(memory_db, f, ensure_ascii=False, indent=4)

def load_memory_from_disk():
    """從硬碟載入記憶，並重新建立向量索引"""
    global memory_db, index
    if os.path.exists(MEMORY_FILE):
        try:
            with open(MEMORY_FILE, 'r', encoding='utf-8') as f:
                content = f.read()
                if not content.strip():
                    memory_db = []
                else:
                    memory_db = json.loads(content)
           
            if memory_db:
                # 提取所有記憶文本進行向量化
                texts = [m['text'] for m in memory_db]
                vectors = embed_model.encode(texts).astype('float32')
                # 重設索引並加入向量
                index = faiss.IndexFlatL2(384)
                index.add(vectors)
                print(f"[SYSTEM] 已從檔案成功載入 {len(memory_db)} 條記憶。")
        except Exception as e:
            print(f"[ERROR] 載入失敗: {e}")
            memory_db = []

def add_memory(content, emotion="中立", importance=1):
    """新增單條記憶並即時同步到索引與檔案"""
    global memory_db, index
    vector = embed_model.encode([content])[0].astype('float32')
    timestamp = datetime.datetime.now().strftime("%Y-%m-%d %H:%M")
   
    entry = {
        "text": content,
        "emotion": emotion,
        "importance": importance,
        "time": timestamp
    }
   
    memory_db.append(entry)
    index.add(np.array([vector]))
    save_to_disk()

def generate_long_term_summary(p_name):
    """每 10 輪對話自動執行摘要，轉化為長期印象，強化 AI 的穩定記憶"""
    if len(memory_db) < 10 or len(memory_db) % 10 != 0:
        return

    # 取最近 10 條對話作為背景
    recent_context = "\n".join([m['text'] for m in memory_db[-10:]])
   
    try:
        resp = client.chat.completions.create(
            model="gpt-4o-mini",
            messages=[
                {"role": "system", "content": f"你是一個記憶整理專家。請總結與玩家{p_name}最近的對話內容，提取關於其性格、愛好或重要承諾。以第三人稱寫成50字內的繁體中文筆記。"},
                {"role": "user", "content": recent_context}
            ]
        )
        summary_text = resp.choices[0].message.content.strip()
        # 長期摘要賦予極高權重 (9)，確保檢索時容易被選中
        add_memory(f"【長期印象】{summary_text}", emotion="感觸", importance=9)
        print(f"[SYSTEM] 長期記憶摘要已生成：{summary_text}")
    except Exception as e:
        print(f"[ERROR] 摘要生成失敗: {e}")

# --- 3. 輔助工具 ---

def clean_text_for_rag(text):
    """移除動作編號與括號，確保記憶文本純淨"""
    text = re.sub(r'\[\d+\]', '', text)
    text = re.sub(r'\(.*?\)', '', text)
    text = re.sub(r'（.*?）', '', text)
    return text.strip()

# --- 4. API 路由 ---
@app.route('/analyze_mbti', methods=['POST'])
def analyze_mbti():
    try:
        data = request.json
        history = data.get('history', '')

        # ⭐ 關鍵：過濾玩家發言
        player_lines = []
        for line in history.split("\n"):
            # 同時支援「玩家」與「User」標籤
            if "玩家:" in line or "User:" in line or "玩家：" in line:
                player_lines.append(line)

        player_only_text = "\n".join(player_lines)

        # 定義 16 型人格對照表，強制要求 AI 使用這些 Title
        mbti_prompt = f"""
你是一位專業心理學家，請「只根據玩家的發言內容」分析其 MBTI 性格。

⚠️ 評分與判斷邏輯（極重要）：
請嚴格遵守以下分數與字母的對應關係來決定最終的 MBTI 類型：
1. EI：0.0 (外向 E) <---> 1.0 (內向 I)
   - 當分數 < 0.5 時，MBTI 第一碼為 'E'；當分數 >= 0.5 時為 'I'。
2. SN：0.0 (實感 S) <---> 1.0 (直覺 N)
   - 當分數 < 0.5 時，MBTI 第二碼為 'S'；當分數 >= 0.5 時為 'N'。
3. TF：0.0 (理性 T) <---> 1.0 (感性 F)
   - 當分數 < 0.5 時，MBTI 第三碼為 'T'；當分數 >= 0.5 時為 'F'。
4. JP：0.0 (判斷 J) <---> 1.0 (感知 P)
   - 當分數 < 0.5 時，MBTI 第四碼為 'J'；當分數 >= 0.5 時為 'P'。

⚠️ 嚴格規則：
1. 絕對不能分析 AI 或角色的性格。
2. 只能根據「玩家說的話」推論。
3. 必須輸出 JSON，不能有任何多餘文字。
4. analysis_text 必須是單行（不能有換行符號 \\n）。
5. 輸出的 "title" 必須嚴格遵循以下對照表：

【對照表】
- INTJ: 建築師 | INTP: 邏輯專家 | ENTJ: 指揮官 | ENTP: 辯論家
- INFJ: 提倡者 | INFP: 調停者 | ENFJ: 主人公 | ENFP: 活動家
- ISTJ: 物流師 | ISFJ: 守護者 | ESTJ: 管理者 | ESFJ: 執政官
- ISTP: 鑑賞家 | ISFP: 冒險家 | ESTP: 企業家 | ESFP: 表演者

【詳細人格判斷標準】
在分析時，請檢索玩家發言是否符合以下特徵：
- E (外向): 表達明快、喜歡分享想法、先行動再思考。傾向大方表現、在互動中得到能量。
- I (內向): 保留想法、注重隱私、先觀察再行動。傾向思考清楚再發言、享受獨處。
- S (實感): 著重具體資訊、細節、已知事實。偏好具體指令、發生過且看得見的事物。
- N (直覺): 抽象思考、掌握大方向、藍圖感。喜歡憑空想像、創造新選項、概念性話題。
- T (理性): 在乎合理性與邏輯、客觀公正、對事不對人。看重事情的一致性與因果。
- F (感性): 在乎人際和諧、情境、他人感受。希望相處中有更多體諒、欣賞與友好態度。
- J (系統): 目標導向、有組織條理、喜歡掌控步調。習慣按部就班、事先安排計畫。
- P (彈性): 隨遇而安、過程導向、保持開放性。接受突發改變、不急著下定論、隨性。

請輸出格式如下：
{{
  "mbti": "ESFP",
  "title": "表演者",
  "analysis_text": "根據玩家...的表現，其展現出...",
  "ei_score": 0.1,
  "sn_score": 0.2,
  "tf_score": 0.7,
  "jp_score": 0.8
}}

說明：
- EI：外向(E) 0.0 ~ 1.0 內向(I)
- SN：感覺(S) 0.0 ~ 1.0 直覺(N)
- TF：思考(T) 0.0 ~ 1.0 情感(F)
- JP：判斷(J) 0.0 ~ 1.0 感知(P)

玩家對話：
{player_only_text}
"""

        resp = client.chat.completions.create(
            model="gpt-4o-mini",
            messages=[
                {
                    "role": "system",
                    "content": "你是一位 MBTI 分析專家，嚴格遵守 JSON 格式與指定的職稱對照表。"
                },
                {
                    "role": "user",
                    "content": mbti_prompt
                }
            ],
            response_format={"type": "json_object"}
        )

        raw_content = resp.choices[0].message.content

        # ⭐ 清理非法控制字元（避免 Unity 解析錯誤）
        clean_content = re.sub(r'[\x00-\x1F\x7F]', '', raw_content)
        result = json.loads(clean_content)

        return jsonify(result)

    except Exception as e:
        print(f"MBTI Analysis Error: {e}")
        return jsonify({
            "mbti": "N/A",
            "title": "分析失敗",
            "analysis_text": "暫時無法分析玩家性格，請輸入更多對話。",
            "ei_score": 0.5,
            "sn_score": 0.5,
            "tf_score": 0.5,
            "jp_score": 0.5
        })
@app.route('/reset_memory', methods=['POST'])
def reset_memory():
    """徹底清除記憶：清空 List、重置索引、刪除檔案"""
    global memory_db, index
    memory_db = []
    index = faiss.IndexFlatL2(384)
    if os.path.exists(MEMORY_FILE):
        os.remove(MEMORY_FILE)
    print("[SYSTEM] 記憶資料庫與實體檔案已完全清空。")
    return jsonify({"status": "success", "message": "Memory cleared completely"})

@app.route('/chat', methods=['POST'])
def chat():
    global chat_counter # 宣告使用全域計數器
    try:
        data = request.json
        u_input = data.get('user_input', "").strip()
        p_name = data.get('player_name', "玩家")
        rules = data.get('system_rules', "")
        # 從請求中取得當前好感度，若無則預設 50
        current_favor = data.get('favorability', 50)

        # --- A. 重要度評估 ---
        # 排除簡單確認語，避免無意義記憶干擾
        if re.search(r"準備好了|開始|好啊|OK|ok|可以|直接出題|沒問題", u_input) or len(u_input) < 2:
            importance_score = 1
        else:
            try:
                score_resp = client.chat.completions.create(
                    model="gpt-4o-mini",
                    messages=[
                        {"role": "system", "content": "判斷這段話對了解玩家長期特質或重要事件的重要性（1-10）。僅回傳數字。"},
                        {"role": "user", "content": u_input}
                    ],
                    max_tokens=5
                )
                score_str = score_resp.choices[0].message.content.strip()
                importance_score = int(''.join(filter(str.isdigit, score_str)) or 1)
            except:
                importance_score = 1

        # --- B. RAG 記憶檢索 (含時間衰減與重要度加權) ---
        context = ""
        if len(memory_db) > 0:
            q_vec = embed_model.encode([u_input])[0].astype('float32').reshape(1, -1)
            # 檢索最相關的 8 條記憶
            dist, idxs = index.search(q_vec, min(8, len(memory_db)))
           
            now = datetime.datetime.now()
            candidates = []
            for i, idx in enumerate(idxs[0]):
                if idx != -1 and idx < len(memory_db):
                    m = memory_db[idx]
                    m_time = datetime.datetime.strptime(m['time'], "%Y-%m-%d %H:%M")
                    hours_passed = (now - m_time).total_seconds() / 3600
                    # 時間衰減：越久的記憶權重越低
                    time_decay = 1.0 / (1.0 + 0.005 * hours_passed)
                   
                    # 評分公式：(重要度 * 時間權重) / (距離 + 修正值)
                    score = (m.get('importance', 1) * time_decay) / (dist[0][i] + 0.1)
                    candidates.append((score, m))
           
            # 取評分最高的前 5 條放入 Prompt
            candidates.sort(key=lambda x: x[0], reverse=True)
            for _, m in candidates[:5]:
                context += f"- ({m['time']}): {m['text']}\n"

        # --- C. 關係語氣動態調整 ---
        favor_style = "保持正常的社交禮儀，語氣自然。"
        if current_favor >= 80:
            favor_style = "【關係：親密】你非常信任玩家，語氣溫柔且帶有依賴感，可以主動提起過去的點滴。"
        elif current_favor <= 25:
            favor_style = "【關係：惡劣】你非常討厭玩家，語氣冷淡、尖銳或不耐煩。"

        # --- D. 生成 AI 回應 ---
        agent_system_prompt = f"""
{rules}

【當前關係狀態】
{favor_style} (當前好感度: {current_favor}/100)

【行為準則】
1. 嚴禁重覆之前說過的話。
2. 檢索到的記憶僅供參考，請優先回應玩家「當下」提出的問題。
3. 若玩家表示聽不懂或要求解釋，請耐心針對該內容進行說明。

【相關背景記憶】
{context if context else "目前尚無相關記憶。"}

【回傳格式要求】
請務必以 JSON 回傳，emotion 使用繁體中文：
{{
  "emotion": "情緒描述",
  "favor_change": 數字,
  "action_id": 數字,
  "reply": "(內心獨白或動作)說的話"
}}
"""

        resp = client.chat.completions.create(
            model="gpt-4o-mini",
            messages=[
                {"role": "system", "content": agent_system_prompt},
                {"role": "user", "content": u_input}
            ],
            response_format={ "type": "json_object" }
        )
       
        result = json.loads(resp.choices[0].message.content)
       
        chat_counter += 1  # 每次對話加 1
       
        # 判斷是否達標：第 5 次，或之後每 5 次 (10, 15, 20...)
        if chat_counter >= 5 and chat_counter % 5 == 0:
            result['trigger_analysis'] = True
        else:
            result['trigger_analysis'] = False
           
        # --- E. 存入記憶與觸發摘要 ---
        # 只有在玩家輸入有實際意義時才存入記憶
        if len(u_input) > 1 and "【特殊指令】" not in u_input:
            clean_reply = clean_text_for_rag(result['reply'])
            memory_text = f"{p_name}對我說「{u_input}」，我回覆「{clean_reply}」"
            add_memory(memory_text, emotion=result['emotion'], importance=importance_score)
           
            # 檢查是否滿 10 輪，若是則執行長期摘要
            generate_long_term_summary(p_name)

        return jsonify(result)

    except Exception as e:
        print(f"Error: {e}")
        return jsonify({
            "reply": "[4] (揉揉太陽穴) 抱歉，我剛才恍神了...你能再說一遍嗎？",
            "action_id": 4,
            "favor_change": 0,
            "emotion": "困惑"
        })

if __name__ == '__main__':
    # 啟動時先嘗試載入既有記憶
    load_memory_from_disk()
    app.run(host='0.0.0.0', port=5000, debug=True)
