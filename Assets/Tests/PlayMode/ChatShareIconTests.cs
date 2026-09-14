using System.Collections;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Forge.Core.Meta;
using Forge.Core.Save;
using Forge.Game;
using Forge.Game.Ui;

namespace Forge.Tests.PlayMode
{
    /// <summary>
    /// T99 — 채팅 공유 카드의 전투력 자리(정본 `ui.js` 5264·5269 `IconGen.img('power') + U.fmt(cp)`)가 «⚔» 글자(글꼴에 없어 □)가 아니라
    /// `power` 아이콘 + 수로 선다. 채팅 씨앗(`Chat.Seed`)이 공유 카드를 하나 끼우므로 채팅을 열기만 하면 카드가 있다.
    /// 그림이 아니라 **계층**으로 본다 — 촬영이 없는 런에서도 도는 단언이다(T89 `ToastIconTests` 와 같은 길).
    /// </summary>
    public class ChatShareIconTests
    {
        private static void DeleteSave()
        {
            try
            {
                string p = System.IO.Path.Combine(Application.persistentDataPath, (SaveIo.Defs != null ? SaveIo.Defs.SaveKey : "forgeclone_save_v1") + ".json");
                if (System.IO.File.Exists(p)) System.IO.File.Delete(p);
            }
            catch (System.Exception) { /* 저장소 접근 실패는 무시 */ }
        }

        [TearDown]
        public void CleanSave() { DeleteSave(); }

        private static IEnumerator Boot()
        {
            DeleteSave();
            SceneManager.LoadScene("SampleScene");
            yield return null;
            yield return null;
            float t = 0f;
            while (!(MetaHost.Ready && PopupLayer.Instance != null) && t < 15f) { t += Time.unscaledDeltaTime; yield return null; }
            Assert.IsTrue(MetaHost.Ready, "MetaHost 가 15초 안에 준비되지 않았다");
            Assert.IsNotNull(PopupLayer.Instance, "팝업 층이 서지 않았다");
            yield return null;
        }

        [UnityTest]
        public IEnumerator 공유_카드의_전투력은_power_아이콘_더하기_수이고_검_글자는_없다()
        {
            yield return Boot();
            Hud.Instance.ChatButton.onClick.Invoke();
            yield return null;
            Assert.IsTrue(PopupLayer.Instance.IsOpen(ChatScreen.Name), "채팅 줄 → 전체화면 채팅");
            Canvas.ForceUpdateCanvases();

            Popup p = PopupLayer.Instance.Find(ChatScreen.Name);
            Assert.IsNotNull(p);
            int cards = 0;
            foreach (RectTransform card in p.Root.GetComponentsInChildren<RectTransform>(true))
            {
                if (card.name != "share") continue;
                cards++;
                foreach (string sideName in new[] { "win", "lose" })
                {
                    Transform side = card.Find(sideName);
                    Assert.IsNotNull(side, "공유 카드에 «" + sideName + "» 쪽이 없다");
                    Transform ico = side.Find("cp-ico");
                    Assert.IsNotNull(ico, sideName + ": 전투력 아이콘 칸(cp-ico)이 없다");
                    Image img = ico.GetComponent<Image>();
                    Assert.IsNotNull(img, sideName + ": cp-ico 에 Image 가 없다");
                    Assert.IsNotNull(img.sprite, sideName + ": power 아이콘 스프라이트가 비었다(T31 아틀라스·카탈로그)");
                    RectTransform ir = (RectTransform)ico;
                    Assert.Greater(ir.rect.width, 0f, sideName + ": 아이콘 칸 폭");
                    Assert.AreEqual(ir.rect.width, ir.rect.height, 0.01f, sideName + ": 아이콘 칸은 정사각");

                    Transform cpTr = side.Find("cp");
                    Assert.IsNotNull(cpTr, sideName + ": 전투력 글자(cp)가 없다");
                    TextMeshProUGUI cp = cpTr.GetComponent<TextMeshProUGUI>();
                    Assert.IsNotNull(cp);
                    Assert.IsFalse(string.IsNullOrEmpty(cp.text), sideName + ": 전투력 수가 비었다");
                    Assert.IsFalse(cp.text.Contains("⚔"), sideName + ": 글자 조각에 ⚔ 가 남았다: «" + cp.text + "»");

                    // 아이콘이 수의 왼쪽에 선다(정본 순서: 아이콘 → 수).
                    Vector3[] a = new Vector3[4], b = new Vector3[4];
                    ir.GetWorldCorners(a);
                    cp.rectTransform.GetWorldCorners(b);
                    Assert.LessOrEqual(a[2].x, b[0].x + 0.5f, sideName + ": 아이콘이 수의 왼쪽에 있어야 한다");
                }
            }
            Assert.GreaterOrEqual(cards, 1, "채팅 씨앗(Chat.Seed)에 공유 카드가 하나는 있어야 한다");

            // 채팅 화면 어느 글자에도 ⚔ 가 없다(이 화면에서 그 글자를 쓰던 자리는 공유 카드뿐이었다).
            foreach (TextMeshProUGUI t in p.Root.GetComponentsInChildren<TextMeshProUGUI>(true))
                Assert.IsFalse(t.text != null && t.text.Contains("⚔"), t.name + " 에 ⚔ 글자가 남았다: «" + t.text + "»");
        }
        /// <summary>정본 `openChat` 은 `pinChatBottom()` 으로 «최신 메시지가 입력바 바로 위» 에 오게 한다 — 씨앗의 공유 카드는 맨 아래서 둘째라 창 안에 있어야 한다
        /// (런 179 채팅 샷은 목록이 11:05~11:20 에 멈춰 카드가 창 밖이었다). 보낸 뒤에는 바닥을 따라간다(정본 `_chatStick`).</summary>
        [UnityTest]
        public IEnumerator 채팅을_열면_목록이_바닥에_붙어_공유_카드가_창_안에_보이고_보낸_뒤에도_바닥을_따라간다()
        {
            yield return Boot();
            MetaHost h = MetaHost.Instance;
            Hud.Instance.ChatButton.onClick.Invoke();
            yield return null;
            Canvas.ForceUpdateCanvases();
            Popup p = PopupLayer.Instance.Find(ChatScreen.Name);
            Assert.IsNotNull(p);

            ScrollRect sr = p.Root.GetComponentInChildren<ScrollRect>(true);
            Assert.IsNotNull(sr, "채팅 목록에 ScrollRect 가 없다");
            RectTransform viewport = sr.viewport;
            Assert.IsNotNull(viewport, "채팅 목록 창(ScrollRect.viewport)이 없다");
            RectTransform card = FindIn(p.Root, "share");
            Assert.IsNotNull(card, "씨앗 공유 카드가 없다");
            Rect vp = WorldRect(viewport), cd = WorldRect(card);
            Assert.IsTrue(cd.yMin >= vp.yMin - 0.5f && cd.yMax <= vp.yMax + 0.5f,
                "공유 카드가 목록 창 밖이다 — 창 y " + vp.yMin + "~" + vp.yMax + " · 카드 y " + cd.yMin + "~" + cd.yMax + " (정본 pinChatBottom)");

            // 마지막 줄(최신 메시지)도 창 안 — 바닥에 붙었다.
            RectTransform content = FindIn(p.Root, "content");
            Assert.IsNotNull(content);
            RectTransform last = LastActiveRow(content);
            Assert.IsNotNull(last, "메시지 줄이 없다");
            Rect lr = WorldRect(last);
            Assert.IsTrue(lr.yMin >= vp.yMin - 0.5f, "최신 메시지가 입력바 아래로 밀려 있다 — 창 yMin " + vp.yMin + " · 줄 yMin " + lr.yMin);

            // 보내면 새 줄이 바닥에 서고 창은 그 줄을 따라간다.
            Assert.IsTrue(h.Chat.SendPlayer(h.ChatState, "안녕", h.Nickname, h.AvatarEmoji, h.Gender, h.NowMs));
            h.Touch();
            yield return null;
            Canvas.ForceUpdateCanvases();
            last = LastActiveRow(content);
            Assert.IsNotNull(last);
            lr = WorldRect(last); vp = WorldRect(viewport);
            Assert.IsTrue(lr.yMin >= vp.yMin - 0.5f && lr.yMax <= vp.yMax + 0.5f,
                "보낸 뒤 최신 메시지가 창 밖이다 — 창 y " + vp.yMin + "~" + vp.yMax + " · 줄 y " + lr.yMin + "~" + lr.yMax);
        }

        /// <summary>T131 — 정본 `ui.js` `chatNameIcons(m)`(msg·share 두 줄 다): 이름 뒤에 [성별 아이콘 gender_m/f][클랜 배지 clanbadge]. 성별은 글자 ♂/♀ 가 아니라 아이콘이고
        /// 배지는 이름 해시(`h*33+c` · `h%3≠0`)인 이름에만 선다. 치수는 정본 `style.css` 3336~3341(PersonIconsUi.json). 해시 벡터는 정본 함수를 node 로 돌린 값.</summary>
        [UnityTest]
        public IEnumerator 채팅_이름줄은_성별_아이콘과_이름_해시_클랜_배지이고_글자_남녀_기호는_없다()
        {
            // 정본 해시 벡터(ui.js chatNameIcons 를 node 로 돌렸다)
            Assert.IsTrue(Chat.ClanBadge("MilkMessiah")); Assert.IsTrue(Chat.ClanBadge("Bearopotamus")); Assert.IsTrue(Chat.ClanBadge("Kite"));
            Assert.IsFalse(Chat.ClanBadge("Yumi")); Assert.IsFalse(Chat.ClanBadge("Jennzee")); Assert.IsFalse(Chat.ClanBadge("Pirimid"));
            Assert.AreEqual("gender_f", Chat.GenderIcon(Chat.GenderFemale)); Assert.AreEqual("gender_m", Chat.GenderIcon(Chat.GenderMale)); Assert.AreEqual("gender_m", Chat.GenderIcon(null));

            yield return Boot();
            Hud.Instance.ChatButton.onClick.Invoke();
            yield return null;
            Canvas.ForceUpdateCanvases();
            Popup p = PopupLayer.Instance.Find(ChatScreen.Name);
            Assert.IsNotNull(p, "채팅 화면이 안 열렸다");
            Rect app = WorldRect(UiRoot.Instance.App);
            int rows = 0, badges = 0;
            foreach (RectTransform nl in p.Root.GetComponentsInChildren<RectTransform>(true))
            {
                if (nl.name != "name-line") continue;
                rows++;
                Transform g = nl.Find("gender");
                Assert.IsNotNull(g, "이름줄에 성별 아이콘 칸(gender)이 없다");
                Image gi = g.GetComponent<Image>();
                Assert.IsNotNull(gi, "gender 는 Image 여야 한다(글자가 아니다)");
                Assert.IsNotNull(gi.sprite, "gender_m/f 스프라이트가 비었다(T31 아틀라스)");
                Rect gr = WorldRect((RectTransform)g);
                Assert.AreEqual(PersonIcons.L("chat_gender_w"), gr.width / app.width, 0.002f, "성별 아이콘 폭 = 앱 폭 × .0370(정본 .chat-gender)");
                Assert.AreEqual(gr.width, gr.height, 0.5f, "성별 아이콘은 정사각");
                TextMeshProUGUI nm = nl.Find("name").GetComponent<TextMeshProUGUI>();
                string name = nm.text; int cut = name.IndexOf("] ", System.StringComparison.Ordinal); if (cut >= 0) name = name.Substring(cut + 2);
                bool want = Chat.ClanBadge(name);
                Transform c = nl.Find("clan");
                Assert.AreEqual(want, c != null, "«" + name + "» 의 클랜 배지 유무가 정본 해시(h%3≠0)와 다르다");
                if (c != null)
                {
                    badges++;
                    Assert.IsNotNull(c.GetComponent<Image>().sprite, "clanbadge 스프라이트가 비었다");
                    Rect cr = WorldRect((RectTransform)c);
                    Assert.AreEqual(PersonIcons.L("chat_clan_w"), cr.width / app.width, 0.002f, "클랜 배지 폭 = 앱 폭 × .0540(정본 .chat-clan)");
                    Assert.Greater(cr.xMin, gr.xMin, "배지는 성별 아이콘 오른쪽");
                }
            }
            Assert.GreaterOrEqual(rows, 1, "메시지 줄이 없다");
            foreach (TextMeshProUGUI t in p.Root.GetComponentsInChildren<TextMeshProUGUI>(true))
                Assert.IsFalse(t.text != null && (t.text.Contains(Chat.GenderMale) || t.text.Contains(Chat.GenderFemale)), t.name + " 에 성별 글자가 남았다: «" + t.text + "»");
        }

        /// <summary>T131 — 프로필 성별 칸(정본 `ui.js` 5043 `IconGen.img(gender_*)` · `.profile-field .ico` 1.15em): 글자 ♂/♀ 가 아니라 아이콘.</summary>
        [UnityTest]
        public IEnumerator 프로필_성별_칸은_아이콘이고_글자_남녀_기호는_없다()
        {
            yield return Boot();
            MetaHost h = MetaHost.Instance;
            ProfilePopup.Open(h);
            yield return null;
            Canvas.ForceUpdateCanvases();
            Popup p = PopupLayer.Instance.Find(ProfilePopup.Name);
            Assert.IsNotNull(p, "프로필 팝업이 안 열렸다");
            RectTransform f = FindIn(p.Root, "gender-field");
            Assert.IsNotNull(f, "성별 칸(gender-field)이 없다");
            Transform ico = f.Find("ico");
            Assert.IsNotNull(ico, "성별 칸 안에 아이콘(ico)이 없다");
            Image img = ico.GetComponent<Image>();
            Assert.IsNotNull(img); Assert.IsNotNull(img.sprite, "gender_m/f 스프라이트가 비었다");
            Rect r = WorldRect((RectTransform)ico), fr = WorldRect(f);
            Assert.AreEqual(r.width, r.height, 0.5f, "아이콘은 정사각");
            Assert.Greater(r.width, 0f);
            Assert.IsTrue(r.xMin >= fr.xMin && r.xMax <= fr.xMax && r.yMin >= fr.yMin - 0.5f && r.yMax <= fr.yMax + 0.5f, "아이콘이 칸 안에 있다");
            Assert.IsNull(f.Find("text"), "성별 칸에 글자 조각이 남았다");
            foreach (TextMeshProUGUI t in p.Root.GetComponentsInChildren<TextMeshProUGUI>(true))
                Assert.IsFalse(t.text != null && (t.text.Contains(Chat.GenderMale) || t.text.Contains(Chat.GenderFemale)), t.name + " 에 성별 글자가 남았다: «" + t.text + "»");
            ProfilePopup.Close(h);
            yield return null;
        }

        /// <summary>T131 2회차 — 플레이어 정보 «성별 · 서버 1» 줄(정본 `ui.js` 5195 `IconGen.img(gender_*) · 서버 1` · CSS 3158 `.clan .ico` 1.05em): 글자 ♂/♀ 가 아니라 아이콘 + 글자.</summary>
        [UnityTest]
        public IEnumerator 플레이어_정보_clan_줄은_성별_아이콘_더하기_서버_글자이고_남녀_기호는_없다()
        {
            yield return Boot();
            float t = 0f;
            while (!ForgeHost.Ready && t < 20f) { t += Time.unscaledDeltaTime; yield return null; }
            MetaHost h = MetaHost.Instance;
            PlayerInfoPopup.Open(h);
            yield return null;
            Canvas.ForceUpdateCanvases();
            Popup p = PopupLayer.Instance.Find(PlayerInfoPopup.Name);
            Assert.IsNotNull(p, "플레이어 정보 팝업이 안 열렸다");
            RectTransform ico = FindIn(p.Root, "clan-ico");
            Assert.IsNotNull(ico, "clan 줄에 성별 아이콘(clan-ico)이 없다");
            Image img = ico.GetComponent<Image>();
            Assert.IsNotNull(img); Assert.IsNotNull(img.sprite, "gender_m/f 스프라이트가 비었다");
            Rect ir = WorldRect(ico);
            Assert.AreEqual(ir.width, ir.height, 0.5f, "아이콘은 정사각");
            Assert.Greater(ir.width, 0f);
            RectTransform clan = FindIn(p.Root, "clan");
            Assert.IsNotNull(clan, "clan 글자가 없다");
            TextMeshProUGUI ct = clan.GetComponent<TextMeshProUGUI>();
            Assert.IsNotNull(ct);
            Assert.IsTrue(ct.text.Contains("서버 1"), "clan 줄 글자 «" + ct.text + "»");
            Rect cr = WorldRect(clan);
            Assert.LessOrEqual(ir.xMax, cr.xMin + 0.5f, "아이콘이 글자 왼쪽에 있다(정본 순서: 아이콘 → · 서버 1)");
            Assert.Less(Mathf.Abs(ir.center.y - cr.center.y), cr.height, "아이콘과 글자가 같은 줄");
            foreach (TextMeshProUGUI tx in p.Root.GetComponentsInChildren<TextMeshProUGUI>(true))
                Assert.IsFalse(tx.text != null && (tx.text.Contains(Chat.GenderMale) || tx.text.Contains(Chat.GenderFemale)), tx.name + " 에 성별 글자가 남았다: «" + tx.text + "»");
            PlayerInfoPopup.Close(h);
            yield return null;
        }

        private static RectTransform FindIn(Transform root, string name)
        {
            foreach (RectTransform rt in root.GetComponentsInChildren<RectTransform>(true)) if (rt.name == name) return rt;
            return null;
        }

        private static RectTransform LastActiveRow(RectTransform content)
        {
            for (int i = content.childCount - 1; i >= 0; i--)
                if (content.GetChild(i).gameObject.activeSelf) return (RectTransform)content.GetChild(i);
            return null;
        }

        private static Rect WorldRect(RectTransform rt)
        {
            Vector3[] c = new Vector3[4];
            rt.GetWorldCorners(c);
            return new Rect(c[0].x, c[0].y, c[2].x - c[0].x, c[2].y - c[0].y);
        }
    }
}
