using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

namespace HorrorGame
{
    public static class DetectionUISetup
    {
        [MenuItem("Tools/Horror Game/Setup Detection Indicator UI")]
        public static void CreateDetectionUI()
        {
            // ---- หา / สร้าง Canvas หลัก ----
            Canvas mainCanvas = FindOrCreateCanvas();
            GameObject canvasGO = mainCanvas.gameObject;

            // ---- ลบ root เก่าถ้ามีแล้ว ----
            Transform existing = canvasGO.transform.Find("DetectionRoot");
            if (existing != null)
            {
                Undo.DestroyObjectImmediate(existing.gameObject);
                Debug.Log("🔄 ลบ DetectionRoot เก่าออก แล้วสร้างใหม่");
            }

            // ---- Root ----
            GameObject root = CreateUIObject("DetectionRoot", canvasGO.transform);
            RectTransform rootRect = root.GetComponent<RectTransform>();
            SetAnchors(rootRect, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 100f));
            rootRect.sizeDelta = new Vector2(220f, 220f);

            // เพิ่ม CanvasGroup สำหรับ fade
            CanvasGroup cg = root.AddComponent<CanvasGroup>();
            cg.alpha = 0f;

            // ---- Arc Background (วงสีเทาจางๆ) ----
            GameObject bgArc = CreateUIObject("ArcBackground", root.transform);
            Image bgImg = bgArc.AddComponent<Image>();
            bgImg.sprite    = CreateArcSprite(128, 160f, 185f, 120, 240);
            bgImg.color     = new Color(1f, 1f, 1f, 0.08f);
            bgImg.type      = Image.Type.Simple;
            bgImg.raycastTarget = false;
            RectTransform bgRect = bgArc.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;

            // ---- Arc Fill (ตัวแสดงการ detect) ----
            GameObject fillArc = CreateUIObject("ArcFill", root.transform);
            Image fillImg = fillArc.AddComponent<Image>();
            fillImg.sprite      = CreateArcSprite(128, 160f, 185f, 120, 240);
            fillImg.color       = new Color(0.85f, 0.6f, 0.2f, 1f);
            fillImg.type        = Image.Type.Filled;
            fillImg.fillMethod  = Image.FillMethod.Radial360;
            fillImg.fillOrigin  = (int)Image.Origin360.Bottom; // เริ่มจากด้านล่าง-กลาง
            fillImg.fillClockwise  = true;
            fillImg.fillAmount  = 0f;
            fillImg.raycastTarget = false;
            RectTransform fillRect = fillArc.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;

            // ---- Eye Icon (ไอคอนตรงกลาง) ----
            GameObject eyeObj = CreateUIObject("EyeIcon", root.transform);
            Image eyeImg = eyeObj.AddComponent<Image>();
            eyeImg.sprite = CreateEyeSprite(64);
            eyeImg.color  = new Color(1f, 1f, 1f, 0.6f);
            eyeImg.raycastTarget = false;
            RectTransform eyeRect = eyeObj.GetComponent<RectTransform>();
            eyeRect.anchorMin = new Vector2(0.5f, 0.5f);
            eyeRect.anchorMax = new Vector2(0.5f, 0.5f);
            eyeRect.anchoredPosition = new Vector2(0f, -10f);
            eyeRect.sizeDelta = new Vector2(40f, 24f);

            // ---- "!" Flash Text ตอนถูกจับ ----
            GameObject alertObj = CreateUIObject("AlertText", root.transform);
            Text alertText = alertObj.AddComponent<Text>();
            alertText.text      = "!";
            alertText.fontSize  = 48;
            alertText.fontStyle = FontStyle.Bold;
            alertText.color     = new Color(1f, 0.15f, 0.1f, 0f);
            alertText.alignment = TextAnchor.MiddleCenter;
            alertText.raycastTarget = false;
            RectTransform alertRect = alertObj.GetComponent<RectTransform>();
            alertRect.anchorMin = new Vector2(0.5f, 0.5f);
            alertRect.anchorMax = new Vector2(0.5f, 0.5f);
            alertRect.anchoredPosition = new Vector2(0f, 30f);
            alertRect.sizeDelta = new Vector2(60f, 60f);

            // ---- แขวน Script ----
            DetectionIndicatorUI uiScript = root.AddComponent<DetectionIndicatorUI>();
            uiScript.arcImage         = fillImg;
            uiScript.alertText        = alertText;
            uiScript.eyeIcon          = eyeImg;
            uiScript.suspiciousColor  = new Color(0.85f, 0.65f, 0.25f, 1f);
            uiScript.detectedColor    = new Color(0.9f, 0.1f, 0.1f, 1f);
            uiScript.safeEyeColor     = new Color(1f, 1f, 1f, 0.35f);
            uiScript.fillSpeed        = 3f;
            uiScript.drainSpeed       = 1.5f;
            uiScript.flashSpeed       = 8f;
            uiScript.scaleDetected    = 1.15f;

            // Register undo
            Undo.RegisterCreatedObjectUndo(root, "Create Detection UI");

            EditorUtility.SetDirty(canvasGO);
            Selection.activeGameObject = root;

            Debug.Log("✅ [DetectionUI] สร้าง Detection Indicator UI เรียบร้อย! 🎮");
            EditorUtility.DisplayDialog(
                "✅ Detection UI สร้างสำเร็จ!",
                "DetectionRoot ถูกเพิ่มใน Canvas แล้ว\n\n" +
                "📌 ตรวจสอบ:\n" +
                "• EnemyAI ต้องอยู่ใน Scene\n" +
                "• ไม่มี Canvas เก่าซ้ำกัน\n\n" +
                "🎨 Arc จะขยับเมื่อ EnemyAI มองเห็นผู้เล่น",
                "OK"
            );
        }

        // ==================== Helper Methods ====================

        static Canvas FindOrCreateCanvas()
        {
            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas != null) return canvas;

            // สร้าง Canvas ใหม่
            GameObject canvasGO = new GameObject("GameCanvas");
            canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGO.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasGO.AddComponent<GraphicRaycaster>();
            Undo.RegisterCreatedObjectUndo(canvasGO, "Create Canvas");
            Debug.Log("🎨 สร้าง Canvas ใหม่");
            return canvas;
        }

        static GameObject CreateUIObject(string name, Transform parent)
        {
            GameObject go = new GameObject(name);
            go.AddComponent<RectTransform>();
            go.transform.SetParent(parent, false);
            return go;
        }

        static void SetAnchors(RectTransform rt, Vector2 min, Vector2 max, Vector2 anchoredPos)
        {
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.pivot     = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPos;
        }

        // สร้าง Arc Sprite โปรแกรมม่าติก (ไม่ต้องใช้ไฟล์ภาพ)
        static Sprite CreateArcSprite(int texSize, float innerR, float outerR, float startDeg, float endDeg)
        {
            Texture2D tex = new Texture2D(texSize, texSize, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            Color[] pixels = new Color[texSize * texSize];
            float center = texSize * 0.5f;

            for (int y = 0; y < texSize; y++)
            {
                for (int x = 0; x < texSize; x++)
                {
                    float dx = x - center;
                    float dy = y - center;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);

                    // มุมจากบนลงล่าง (0° = ขึ้น, clockwise)
                    float angle = Mathf.Atan2(dx, dy) * Mathf.Rad2Deg; // 0=top, +ve=right
                    angle = (angle + 360f) % 360f;

                    bool inRing = dist >= innerR * (texSize / 200f) && dist <= outerR * (texSize / 200f);
                    bool inArc  = IsAngleInArc(angle, startDeg, endDeg);

                    if (inRing && inArc)
                    {
                        // anti-alias ขอบวงใน/นอก
                        float innerEdge = innerR * (texSize / 200f);
                        float outerEdge = outerR * (texSize / 200f);
                        float aa = Mathf.Min(
                            Mathf.InverseLerp(innerEdge - 1.5f, innerEdge + 1.5f, dist),
                            Mathf.InverseLerp(outerEdge + 1.5f, outerEdge - 1.5f, dist)
                        );
                        pixels[y * texSize + x] = new Color(1f, 1f, 1f, aa);
                    }
                    else
                    {
                        pixels[y * texSize + x] = Color.clear;
                    }
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, texSize, texSize), new Vector2(0.5f, 0.5f), texSize);
        }

        static bool IsAngleInArc(float angle, float start, float end)
        {
            if (start <= end) return angle >= start && angle <= end;
            return angle >= start || angle <= end;
        }

        // สร้าง Eye Sprite (รูปตา) แบบโปรแกรมม่าติก
        static Sprite CreateEyeSprite(int size)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[size * size];
            float cx = size * 0.5f, cy = size * 0.5f;
            float w = size * 0.42f, h = size * 0.28f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float nx = (x - cx) / w;
                    float ny = (y - cy) / h;
                    float ellipse = nx * nx + ny * ny;
                    float pupilR = ((x - cx) * (x - cx) + (y - cy) * (y - cy)) / (size * size * 0.04f);

                    float a = 0f;
                    if (ellipse <= 1f)
                    {
                        a = 1f - Mathf.InverseLerp(0.85f, 1.05f, ellipse); // ขอบ aa
                        if (pupilR <= 1f) a = 1f; // ตาดำ
                    }
                    pixels[y * size + x] = new Color(1f, 1f, 1f, a);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }
    }
}
