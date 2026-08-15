using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using CAT.Utility;

/// <summary>
/// PathRibbon 인스펙터.
/// - 모드 / 자식 렌더러 / 샘플 개수 / 타일 길이 등 런타임 상태 표시
/// - 자식 설정 오류 시 경고 출력
/// - Rebuild 버튼 제공
/// </summary>
[CustomEditor(typeof(PathRibbon))]
[CanEditMultipleObjects]
public class PathRibbonEditor : Editor
{
        private SerializedProperty _enableScrollProp;
        private SerializedProperty _invertScrollProp;
        private SerializedProperty _samplesPerUnitProp;
        private SerializedProperty _overrideSamplesProp;
        private SerializedProperty _manualSamplesProp;
        private SerializedProperty _autoSubCanvasProp;
        private SerializedProperty _flipXProp;
        private SerializedProperty _flipYProp;

        private static readonly GUIContent LabelEnableScroll  = new GUIContent("Scroll (Conveyor)", "리본 텍스처를 컨베이어처럼 흐르게 합니다. 속도는 PathFollower Duration 에서 자동 계산 (경로 길이 ÷ Duration). Loop 경로에서만 동작");
        private static readonly GUIContent LabelInvertScroll  = new GUIContent("Invert Direction", "스크롤 방향 반전");
        private static readonly GUIContent LabelSamplesPerU   = new GUIContent("Samples / Unit", "경로 1유닛당 정점 개수 (자동 모드)");
        private static readonly GUIContent LabelOverride      = new GUIContent("Override Samples", "샘플 개수를 수동으로 지정");
        private static readonly GUIContent LabelManualSamples = new GUIContent("Manual Samples", "수동 샘플 개수");
        private static readonly GUIContent LabelAutoSubCanvas = new GUIContent("Auto Sub Canvas", "UI 모드에서 서브 Canvas 를 자동 추가하여 상위 Canvas rebuild 격리 (UV 스크롤/모핑 사용 시 권장)");
        private static readonly GUIContent LabelFlipX         = new GUIContent("Flip X", "가로(경로 방향) 반전. Sprite 모드에서는 자식 SpriteRenderer.flipX 와 XOR 결합");
        private static readonly GUIContent LabelFlipY         = new GUIContent("Flip Y", "세로(리본 두께) 반전. Sprite 모드에서는 자식 SpriteRenderer.flipY 와 XOR 결합");

        private void OnEnable()
        {
            _enableScrollProp   = serializedObject.FindProperty(nameof(PathRibbon.enableScroll));
            _invertScrollProp   = serializedObject.FindProperty(nameof(PathRibbon.invertScroll));
            _samplesPerUnitProp = serializedObject.FindProperty(nameof(PathRibbon.samplesPerUnit));
            _overrideSamplesProp= serializedObject.FindProperty(nameof(PathRibbon.overrideSamples));
            _manualSamplesProp  = serializedObject.FindProperty(nameof(PathRibbon.manualSamples));
            _autoSubCanvasProp  = serializedObject.FindProperty(nameof(PathRibbon.autoCreateSubCanvas));
            _flipXProp          = serializedObject.FindProperty(nameof(PathRibbon.flipX));
            _flipYProp          = serializedObject.FindProperty(nameof(PathRibbon.flipY));
        }

        public override void OnInspectorGUI()
        {
            var ribbon = (PathRibbon)target;

            serializedObject.Update();

            // ── 기본 설정 ──
            EditorGUILayout.LabelField("Ribbon", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_enableScrollProp, LabelEnableScroll);
            using (new EditorGUI.DisabledScope(!_enableScrollProp.boolValue))
            {
                EditorGUILayout.PropertyField(_invertScrollProp, LabelInvertScroll);
            }

            // Flip 가로/세로 — 가로형 토글 레이아웃
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PropertyField(_flipXProp, LabelFlipX);
                EditorGUILayout.PropertyField(_flipYProp, LabelFlipY);
            }

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Mesh Resolution", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_overrideSamplesProp, LabelOverride);
            using (new EditorGUI.DisabledScope(_overrideSamplesProp.boolValue))
            {
                EditorGUILayout.PropertyField(_samplesPerUnitProp, LabelSamplesPerU);
            }
            using (new EditorGUI.DisabledScope(!_overrideSamplesProp.boolValue))
            {
                EditorGUILayout.PropertyField(_manualSamplesProp, LabelManualSamples);
            }

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Performance (Mobile)", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_autoSubCanvasProp, LabelAutoSubCanvas);

            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space(6);
            DrawRuntimeInfo(ribbon);

            EditorGUILayout.Space(6);
            DrawWarnings(ribbon);

            EditorGUILayout.Space(6);
            if (GUILayout.Button("Rebuild Mesh"))
            {
                foreach (var t in targets)
                {
                    if (t is PathRibbon pr) pr.RebuildMesh();
                }
                SceneView.RepaintAll();
            }
        }

        private void DrawRuntimeInfo(PathRibbon ribbon)
        {
            EditorGUILayout.LabelField("Runtime Info", EditorStyles.boldLabel);

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.Toggle("UI Mode (auto)", ribbon.IsUIMode);
                EditorGUILayout.IntField("Sample Count", ribbon.ActualSampleCount);
                EditorGUILayout.FloatField("Total Path Length", ribbon.TotalPathLength);
                EditorGUILayout.FloatField("Effective Tile Length", ribbon.EffectiveTileLength);

                // 자동 계산된 스크롤 속도 표시 (경로 길이 ÷ PathFollower Duration)
                var follower = ribbon.GetComponent<PathFollower>();
                float autoSpeed = (follower != null && follower.duration > 1e-4f)
                    ? ribbon.TotalPathLength / follower.duration
                    : 0f;
                EditorGUILayout.FloatField("Scroll Speed (auto)", autoSpeed);
            }
        }

        private void DrawWarnings(PathRibbon ribbon)
        {
            // 스크롤은 닫힌(Loop) 경로에서만 동작
            if (ribbon.enableScroll)
            {
                var follower = ribbon.GetComponent<PathFollower>();
                if (follower != null && !follower.IsLoop)
                {
                    EditorGUILayout.HelpBox(
                        "스크롤(컨베이어)은 닫힌(Loop) 경로에서만 동작합니다. PathFollower 의 Loop 를 켜세요.",
                        MessageType.Warning);
                }
            }

            // 자식 렌더러 검사
            bool hasChildRenderer = false;
            SpriteRenderer foundSR = null;
            Image foundImg = null;

            int cc = ribbon.transform.childCount;
            for (int i = 0; i < cc; i++)
            {
                var c = ribbon.transform.GetChild(i);
                if (ribbon.IsUIMode)
                {
                    var img = c.GetComponent<Image>();
                    if (img != null) { foundImg = img; hasChildRenderer = true; break; }
                }
                else
                {
                    var sr = c.GetComponent<SpriteRenderer>();
                    if (sr != null) { foundSR = sr; hasChildRenderer = true; break; }
                }
            }

            if (!hasChildRenderer)
            {
                EditorGUILayout.HelpBox(
                    ribbon.IsUIMode
                        ? "자식 오브젝트에 Image (Type=Tiled) 컴포넌트를 배치하세요."
                        : "자식 오브젝트에 SpriteRenderer (Draw Mode=Tiled) 컴포넌트를 배치하세요.",
                    MessageType.Warning);
                return;
            }

            // Draw Mode / Type 검사
            if (foundSR != null && foundSR.drawMode != SpriteDrawMode.Tiled)
            {
                EditorGUILayout.HelpBox(
                    "자식 SpriteRenderer의 Draw Mode 가 Tiled 가 아닙니다. Size 필드가 필요하므로 Tiled 로 변경하세요.",
                    MessageType.Warning);
            }
            if (foundImg != null && foundImg.type != Image.Type.Tiled)
            {
                EditorGUILayout.HelpBox(
                    "자식 Image의 Type 이 Tiled 가 아닙니다. Type=Tiled 로 변경하세요.",
                    MessageType.Warning);
            }

            // 리본 두께 vs 경로 곡률 검사 (Sprite 모드, Tiled)
            // Tiled 전환 직후 Size 는 스프라이트 크기와 무관한 (1,1)이 기본값이라,
            // 작은 경로에서는 두께 절반이 곡률 반경을 넘어 메시가 자가 교차(별/부채꼴 붕괴)하기 쉽다.
            if (foundSR != null && foundSR.drawMode == SpriteDrawMode.Tiled && foundSR.sprite != null)
            {
                var pathFollower = ribbon.GetComponent<PathFollower>();
                if (pathFollower != null && pathFollower.PointCount >= 2)
                {
                    float minRadius = EstimateMinCurvatureRadius(pathFollower, ribbon.transform.parent);
                    float halfWidth = foundSR.size.y * 0.5f;

                    if (minRadius < float.MaxValue && halfWidth > minRadius)
                    {
                        EditorGUILayout.HelpBox(
                            $"리본 두께(자식 Size Y = {foundSR.size.y:F2})의 절반이 경로 최소 곡률 반경({minRadius:F2})보다 큽니다.\n" +
                            "안쪽 정점이 곡률 중심을 넘어가 메시가 자가 교차(별/부채꼴 모양으로 붕괴)합니다.\n" +
                            "Size Y 를 줄이거나 경로를 크게 만드세요.",
                            MessageType.Warning);
                    }

                    // Size 가 스프라이트 원본 크기와 다르면 원클릭 보정 버튼 제공
                    Sprite sp = foundSR.sprite;
                    Vector2 native = new Vector2(
                        sp.rect.width  / Mathf.Max(0.0001f, sp.pixelsPerUnit),
                        sp.rect.height / Mathf.Max(0.0001f, sp.pixelsPerUnit));
                    if (Vector2.Distance(foundSR.size, native) > 1e-4f)
                    {
                        if (GUILayout.Button($"자식 Size를 스프라이트 원본 크기로 ({native.x:F2} × {native.y:F2})"))
                        {
                            Undo.RecordObject(foundSR, "Set Sprite Native Size");
                            foundSR.size = native;
                            EditorUtility.SetDirty(foundSR);
                            ribbon.MarkDirty();
                            SceneView.RepaintAll();
                        }
                    }
                }
            }

            // URP 2D Sprite 셰이더는 MeshRenderer 비호환 → 폴백 material 자동 대체 안내
            if (foundSR != null && PathRibbon.IsSpriteOnlyShader(foundSR.sharedMaterial))
            {
                EditorGUILayout.HelpBox(
                    "자식 SpriteRenderer 의 material(URP 2D Sprite 셰이더)은 MeshRenderer 에서 렌더링되지 않으므로, " +
                    "PathRibbon 전용 폴백 material(CAT/PathFollower/Ribbon-Unlit)로 자동 대체됩니다.",
                    MessageType.Info);
            }

            // 텍스처 Wrap Mode 검사
            Texture tex = null;
            if (foundSR != null && foundSR.sprite != null) tex = foundSR.sprite.texture;
            if (foundImg != null && foundImg.sprite != null) tex = foundImg.sprite.texture;

            if (tex != null && tex.wrapMode != TextureWrapMode.Repeat && tex.wrapMode != TextureWrapMode.MirrorOnce && tex.wrapMode != TextureWrapMode.Mirror)
            {
                EditorGUILayout.HelpBox(
                    "Sprite의 Texture Wrap Mode 가 Repeat 이 아니면 타일링이 끊어질 수 있습니다. Texture Import 설정에서 Wrap Mode = Repeat 로 변경하세요.",
                    MessageType.Info);
            }
        }

        /// <summary>
        /// 경로의 최소 곡률 반경을 경로 공간(부모 로컬) 기준으로 추정한다.
        /// 연속 샘플 간 호 길이(ds)와 접선 회전각(dθ)으로 반경 = ds/dθ 근사.
        /// </summary>
        private static float EstimateMinCurvatureRadius(PathFollower follower, Transform parentSpace)
        {
            Matrix4x4 worldToPath = parentSpace != null ? parentSpace.worldToLocalMatrix : Matrix4x4.identity;
            const int SampleCount = 64;

            Vector3 prevPos = worldToPath.MultiplyPoint3x4(follower.GetPointAt(0f));
            Vector3 prevDir = worldToPath.MultiplyVector(follower.GetDirectionAt(0f)).normalized;
            float minRadius = float.MaxValue;

            for (int i = 1; i <= SampleCount; i++)
            {
                float t = (float)i / SampleCount;
                Vector3 pos = worldToPath.MultiplyPoint3x4(follower.GetPointAt(t));
                Vector3 dir = worldToPath.MultiplyVector(follower.GetDirectionAt(t)).normalized;

                float ds     = (pos - prevPos).magnitude;
                float dTheta = Vector3.Angle(prevDir, dir) * Mathf.Deg2Rad;
                if (dTheta > 1e-4f && ds > 1e-6f)
                    minRadius = Mathf.Min(minRadius, ds / dTheta);

                prevPos = pos;
                prevDir = dir;
            }
            return minRadius;
        }
    }
