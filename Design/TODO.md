# TODO (z 17. 11. 2026)

## 1. Rewarded reklama: gemy navíc (dobrovolně) — HOTOVO 18. 11. 2026

Malé tlačítko (ikona filmu `button_31`) vpravo vedle gemů na výsledkové tabuli:
- výhra v kampani, konec TimeMode a LifeMode: „2×“ (gemy ze hry ještě jednou),
- prohra v kampani: „+N“ (gemy z levelu, které by jinak propadly),
- na pauze a ve finále (bez tabule) se nenabízí.

Kód: `Rewarded.cs` (singleton přes všechny scény, sám se načítá), `ResultScoreScript.OfferRewardedGems`.
Odměna jen za dokoukanou reklamu, po přeskočení nabídka zůstane. Tlačítko se ukáže, jen když
je reklama načtená. Běží i s `NoAds`, protože je dobrovolná.

AdMob jednotka `ca-app-pub-2771443006207374/9130330574`. V Editoru a **Development Build**
jede testovací jednotka Googlu, skutečná jen v release buildu (bez zaškrtnutého Development Build).

Nápad na později: po prohře v kampani „Pokračovat“ za reklamu (+1 život / +30 s).

## 2. Nálezy z prohlídky projektu

Podle priority:

1. **Chybějící grafika v kampani** (`Design/MissingArt.txt`): Mandrake a pozadí Street,
   Clinic, WarCamp, Dungeon jsou hotové (18. 11.). Kniha je složená ze dvou pergamenů
   (`UI board Large parchment`) na tmavém podkladu `Campaign/Book.png`, vypadá dobře.
2. **Monetizace:**
   - `NoAds` se v `AdIns.cs` kontroluje, ale nic ho nenastaví, takže odstranění
     reklam nejde koupit. V projektu není žádný nákup za skutečné peníze (IAP).
   - Nápad: nákup „Remove ads“ a balíčky gemů do obchodu s kosmetikou.
3. ~~**Tažení bylinek**~~ HOTOVO (18. 11.): `HerbItemScript` jede přes `IDragHandler`
   a pozici z `eventData`, drží jen prst, který bylinku vzal. Funguje i myší.
4. **Drobné chyby:**
   - ~~`AdIns.Awake` → `NullReferenceException` při každém načtení scény~~ HOTOVO (18. 11.).
   - ~~`AchievementNotification.cs` má `using NUnit.Framework;`~~ HOTOVO (18. 11.).
5. **Jazyk:** hra je jen anglicky. Zvážit lokalizaci aspoň menu a tutoriálu.
6. ~~**Souhlas s reklamami (GDPR)**~~ HOTOVO (18. 11.): `AdsConsent.cs` při startu ukáže dialog
   z AdMobu (Soukromí a zprávy), reklamy (`AdIns`, `Rewarded`) se načítají až po něm přes
   `AdsConsent.WhenReady`. V nastavení je tlačítko „Privacy options“ (jen kde to Google vyžaduje).
   V Editoru je UMP jen zástupný, skutečný dialog jde vyzkoušet jen na telefonu.
7. **Web vývojáře** https://filiplukes.github.io (`app-ads.txt`, zásady na `/privacy/`) je
   potřeba zadat do Google Play Console: web vývojáře i URL zásad ochrany osobních údajů.

8. **Hodnocení ve hře** HOTOVO (18. 11.): `InAppReview.cs` (Google Play In-App Review, knihovna
   `com.google.android.play:review` přes `Script/Editor/InAppReviewDependencies.xml`). Po výhře
   v kampani a na konci TimeMode/LifeMode: poprvé po 3. hře, podruhé nejdřív za 3 dny, pak už nikdy.
   Když přijde na řadu v TimeMode/LifeMode, interstitial se ten konec neukáže.
   Okno jde vidět jen ve hře z Google Play (interní testování), ne v buildu nahraném přes kabel.

9. **Prohlídka hry 18. 11.** HOTOVO:
   - Jména v žebříčku: `NameRules` (mezery → podtržítko, max. 16 znaků, filtr sprostých slov).
     Unity jméno s mezerou odmítala, hráč pak měl náhodné jméno. Opraví se i staré uložené
     jméno při dalším spuštění. Sprostá jména se v žebříčku ukazují jako „Alchemist“.
   - Žebříček: dlouhá jména zkrácená „…“, mezera před skóre.
   - Achievementy: název na jeden řádek (zmenší se), nepřetéká přes ikonu a okraj.
   - Název receptu v HUD (TimeMode, LifeMode, Campaign): jeden řádek, zmenší se.
   - Kniha receptů: tmavé pozadí přes menu, tlačítka menu pod ní nejdou stisknout.
   - Nechané být: `AdIns` má skutečné ID i ve vývojových buildech (neklikat na vlastní reklamy).

## Shrnutí hodnocení

Silné: originální jádro (byliny + obkreslování run), hloubka (24 receptů, 12 živlů,
kvalita a série, zákazníci, otrava), kampaň se 16 levely, 8 postavami a 3 konci,
retence (achievementy, žebříčky, 15 kosmetik za gemy), čitelný datově řízený kód.

Před vydáním kampaně: doplnit grafiku (hlavně Mandrake) a opravit tažení bylinek.
Největší přínos pro výdělek: rewarded 2× gemy a nákup odstranění reklam.
