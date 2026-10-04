using System.Collections;
using System.Reflection;
using NUnit.Framework;
using StarterProject.UI;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace StarterProject.Tests
{
    public sealed class StarterCanvasLayoutTests
    {
        [UnityTest]
        public IEnumerator LandscapeCanvasFitsInteractiveContentInsideSafeArea()
        {
            var root = new GameObject("Layout Test", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            var background = CreateRect(root.transform, "Background", true);
            var button = CreateRect(root.transform, "Action", false);
            var canvas = root.GetComponent<Canvas>();
            var layout = StarterCanvasLayout.EnsureConfigured(canvas);

            Assert.That(canvas.renderMode, Is.EqualTo(RenderMode.ScreenSpaceOverlay));
            Assert.That(root.GetComponent<CanvasScaler>().uiScaleMode,
                Is.EqualTo(CanvasScaler.ScaleMode.ScaleWithScreenSize));
            Assert.That(root.GetComponent<CanvasScaler>().referenceResolution,
                Is.EqualTo(new Vector2(1920, 1080)));
            Assert.That(background.parent, Is.EqualTo(root.transform), "The background must cover the cutout area.");
            Assert.That(button.parent, Is.EqualTo(layout.ContentRoot), "Controls must stay inside the safe area.");

            // 2400x1080 가로 화면, 양쪽 80px 노치 여백과 별도 Canvas 배율.
            layout.ApplyLayout(2400, 1080, new Rect(80, 0, 2240, 1080), 1.125f);
            Assert.That(layout.SafeAreaRoot.anchorMin.x, Is.EqualTo(80f / 2400f).Within(0.0001f));
            Assert.That(layout.SafeAreaRoot.anchorMax.x, Is.EqualTo(2320f / 2400f).Within(0.0001f));
            Assert.That(layout.ContentRoot.localScale.x, Is.EqualTo(8f / 9f).Within(0.0001f));

            Object.Destroy(root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator DesktopResolutionChangeReflowsWithoutDuplicatingHierarchy()
        {
            var root = new GameObject("Layout Test", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            var button = CreateRect(root.transform, "Action", false);
            var layout = StarterCanvasLayout.EnsureConfigured(root.GetComponent<Canvas>());
            layout.ApplyLayout(1920, 1080, new Rect(0, 0, 1920, 1080), 1f);
            Assert.That(layout.ContentRoot.localScale.x, Is.EqualTo(1f).Within(0.0001f));

            layout.ApplyLayout(960, 540, new Rect(0, 0, 960, 540), 0.5f);
            Assert.That(layout.ContentRoot.localScale.x, Is.EqualTo(1f).Within(0.0001f));
            Assert.That(StarterCanvasLayout.EnsureConfigured(root.GetComponent<Canvas>()), Is.SameAs(layout));
            Assert.That(root.transform.childCount, Is.EqualTo(1));
            Assert.That(button.parent, Is.EqualTo(layout.ContentRoot));

            Object.Destroy(root);
            yield return null;
        }

        [Test]
        public void FullScreenPanelKeepsItsControlsInsideSafeArea()
        {
            var root = new GameObject("Panel Layout Test", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            try
            {
                var background = CreateRect(root.transform, "Background", true);
                var panel = CreateRect(root.transform, "Menu Panel", true);
                var button = CreateRect(panel, "Menu Button", false);
                button.gameObject.AddComponent<Button>();
                var fullScreenButton = CreateRect(root.transform, "Full Screen Button", true);
                fullScreenButton.gameObject.AddComponent<Button>();

                var layout = StarterCanvasLayout.EnsureConfigured(root.GetComponent<Canvas>());
                layout.ApplyLayout(2400, 1080, new Rect(80, 0, 2240, 1080), 1.125f);

                Assert.That(background.parent, Is.EqualTo(root.transform));
                Assert.That(panel.parent, Is.EqualTo(layout.ContentRoot));
                Assert.That(button.IsChildOf(layout.ContentRoot), Is.True);
                Assert.That(fullScreenButton.parent, Is.EqualTo(layout.ContentRoot));
                Assert.That(layout.SafeAreaRoot.anchorMin.x, Is.EqualTo(80f / 2400f).Within(0.0001f));
                Assert.That(layout.ContentRoot.localScale.x, Is.EqualTo(8f / 9f).Within(0.0001f));
            }
            finally { Object.DestroyImmediate(root); }
        }

        [Test]
        public void ExplicitBackdropContainerRemainsOutsideSafeArea()
        {
            var root = new GameObject("Backdrop Layout Test", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            root.SetActive(false);
            try
            {
                var backdrop = CreateRect(root.transform, "Layered Background", true);
                var decoration = CreateRect(backdrop, "Decoration", false);
                var panel = CreateRect(root.transform, "Menu Panel", true);
                CreateRect(panel, "Label", false);
                var layout = root.AddComponent<StarterCanvasLayout>();
                var backdropField = typeof(StarterCanvasLayout).GetField("fullScreenRoots", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(backdropField, Is.Not.Null);
                backdropField.SetValue(layout, new[] { backdrop });

                StarterCanvasLayout.EnsureConfigured(root.GetComponent<Canvas>());

                Assert.That(backdrop.parent, Is.EqualTo(root.transform));
                Assert.That(decoration.parent, Is.EqualTo(backdrop));
                Assert.That(panel.parent, Is.EqualTo(layout.ContentRoot));
                Assert.That(StarterCanvasLayout.EnsureConfigured(root.GetComponent<Canvas>()), Is.SameAs(layout));
                Assert.That(root.transform.childCount, Is.EqualTo(2));
            }
            finally { Object.DestroyImmediate(root); }
        }

        [TestCase(0f)]
        [TestCase(0.5f)]
        [TestCase(1f)]
        public void CustomLoadingBarFillsTrackFromLeftAtReportedProgress(float progress)
        {
            var track = new GameObject("Custom Track", typeof(RectTransform)).GetComponent<RectTransform>();
            try
            {
                track.sizeDelta = new Vector2(400, 24);
                var fill = new GameObject("Custom Fill", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
                fill.transform.SetParent(track, false);
                // Inspector에서 만든 기본 Image처럼 중앙 앵커·고정 크기를 사용합니다.
                var rect = fill.rectTransform;
                rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(100, 100);
                rect.anchoredPosition = new Vector2(15, 8);

                StarterLoadingOverlay.UpdateProgressFill(fill, progress);

                var trackCorners = new Vector3[4];
                var fillCorners = new Vector3[4];
                track.GetWorldCorners(trackCorners);
                rect.GetWorldCorners(fillCorners);
                Assert.That(rect.rect.width, Is.EqualTo(400 * progress).Within(0.001f));
                Assert.That(rect.rect.height, Is.EqualTo(24).Within(0.001f));
                Assert.That(fillCorners[0].x, Is.EqualTo(trackCorners[0].x).Within(0.001f));
                Assert.That(fillCorners[0].y, Is.EqualTo(trackCorners[0].y).Within(0.001f));
                Assert.That(fillCorners[1].y, Is.EqualTo(trackCorners[1].y).Within(0.001f));
            }
            finally { Object.DestroyImmediate(track.gameObject); }
        }

        [TestCase(0f)]
        [TestCase(0.5f)]
        [TestCase(1f)]
        public void FilledLoadingImageKeepsAuthoredShapeAndLayout(float progress)
        {
            var fill = new GameObject("Custom Radial Fill", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            try
            {
                fill.type = Image.Type.Filled;
                fill.fillMethod = Image.FillMethod.Radial360;
                fill.fillOrigin = (int)Image.Origin360.Top;
                fill.fillClockwise = false;
                var rect = fill.rectTransform;
                rect.anchorMin = new Vector2(0.2f, 0.3f);
                rect.anchorMax = new Vector2(0.7f, 0.8f);
                rect.offsetMin = new Vector2(12, 24);
                rect.offsetMax = new Vector2(-16, -28);
                var anchorMin = rect.anchorMin;
                var anchorMax = rect.anchorMax;
                var offsetMin = rect.offsetMin;
                var offsetMax = rect.offsetMax;

                StarterLoadingOverlay.UpdateProgressFill(fill, progress);

                Assert.That(fill.fillAmount, Is.EqualTo(progress).Within(0.001f));
                Assert.That(fill.fillMethod, Is.EqualTo(Image.FillMethod.Radial360));
                Assert.That(fill.fillOrigin, Is.EqualTo((int)Image.Origin360.Top));
                Assert.That(fill.fillClockwise, Is.False);
                Assert.That(rect.anchorMin, Is.EqualTo(anchorMin));
                Assert.That(rect.anchorMax, Is.EqualTo(anchorMax));
                Assert.That(rect.offsetMin, Is.EqualTo(offsetMin));
                Assert.That(rect.offsetMax, Is.EqualTo(offsetMax));
            }
            finally { Object.DestroyImmediate(fill.gameObject); }
        }

        private static RectTransform CreateRect(Transform parent, string name, bool fullScreen)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = fullScreen ? Vector2.zero : new Vector2(0.5f, 0.5f);
            rect.anchorMax = fullScreen ? Vector2.one : new Vector2(0.5f, 0.5f);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            return rect;
        }
    }
}
