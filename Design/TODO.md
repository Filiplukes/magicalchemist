# TODO (z 17. 11. 2026)

## 1. Rewarded reklama: dvojnásobné gemy (dobrovolně)

Po výhře v kampani (a případně i na konci TimeMode/LifeMode) tlačítko navíc na výsledkové
tabuli: „Zhlédni reklamu a získej 2× gemy“. Jen dobrovolně, žádné nucené reklamy v kampani.

Stav: `Assets/Script/Rewarded.cs` je jen nefunkční kostra z jiného projektu:
- celý kód je zakomentovaný, nic se nenačítá ani nezobrazuje,
- po zhlédnutí volá neexistující `InGameManager`, odměna je jen `// TODO`,
- skript není připojený v žádné scéně ani prefabu.

Než se začne:
- [ ] Zjistit v AdMob, jestli existuje **rewarded** jednotka pro tuhle aplikaci
      (app ID `ca-app-pub-2771443006207374~3234940876`). ID v `Rewarded.cs`
      (`…/5393647118`) může patřit k jiné hře.
- [ ] Do té doby vyvíjet s testovací jednotkou Googlu a před vydáním ji vyměnit.

Návrh: tlačítko na `ResultScore` prefabu, `ResultScoreScript` po odměně připíše druhou
dávku gemů a aktualizuje číslo. Tlačítko schovat, když reklama není načtená, nebo když
má hráč `NoAds`.

Další nápad na později: po prohře v kampani „Pokračovat“ za reklamu (+1 život / +30 s).

## 2. Nálezy z prohlídky projektu

Podle priority:

1. **Chybějící grafika v kampani** (`Design/MissingArt.txt`): Mandrake je černý čtverec
   (a je klíčová ve finále), chybí kniha a pozadí Street, Clinic, WarCamp, Dungeon.
2. **Monetizace:**
   - `NoAds` se v `AdIns.cs` kontroluje, ale nic ho nenastaví, takže odstranění
     reklam nejde koupit. V projektu není žádný nákup za skutečné peníze (IAP).
   - Nápad: nákup „Remove ads“ a balíčky gemů do obchodu s kosmetikou.
3. **Tažení bylinek** (`HerbItemScript`) čte jen `Touchscreen.current.primaryTouch`.
   Při druhém prstu nebo zaseknutém dotyku bylinka uletí jinam. Přepsat na
   `IDragHandler` s pozicí z `eventData` (funguje pak i myší v Game view).
4. **Drobné chyby:**
   - `AdIns.Awake` přidává `OnAdFullScreenContentClosed` na `interstitial`, který je
     v tu chvíli ještě `null` → `NullReferenceException` při každém načtení scény.
   - `AchievementNotification.cs` má `using NUnit.Framework;` v herním kódu,
     hrozí pád player buildu.
5. **Jazyk:** hra je jen anglicky. Zvážit lokalizaci aspoň menu a tutoriálu.

## Shrnutí hodnocení

Silné: originální jádro (byliny + obkreslování run), hloubka (24 receptů, 12 živlů,
kvalita a série, zákazníci, otrava), kampaň se 16 levely, 8 postavami a 3 konci,
retence (achievementy, žebříčky, 15 kosmetik za gemy), čitelný datově řízený kód.

Před vydáním kampaně: doplnit grafiku (hlavně Mandrake) a opravit tažení bylinek.
Největší přínos pro výdělek: rewarded 2× gemy a nákup odstranění reklam.
