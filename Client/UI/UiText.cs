using System;
using System.Collections.Generic;
using EFT;

namespace ImprovedTabUI.UI
{
    internal static class UiText
    {
        internal const int ResetToDefault = 0;
        internal const int KeepOneVisible = 1;
        internal const int Mod = 2;
        internal const int Bar = 3;
        internal const int Wheel = 4;
        internal const int Slots = 5;
        internal const int OffWheel = 6;
        internal const int ResetKeys = 7;
        internal const int ResetBarTip = 8;
        internal const int ResetWheelTip = 9;
        internal const int ResetKeysTip = 10;
        internal const int BarTip = 11;
        internal const int WheelTip = 12;
        internal const int KeysInRaid = 13;
        internal const int KeysInRaidTip = 14;

        private static readonly Dictionary<string, string[]> Languages = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            { "en", new[] { "RESET TO DEFAULT", "KEEP ONE VISIBLE", "MOD", "BAR", "WHEEL", "SLOTS", "NOT ON THE WHEEL", "RESET KEYBINDS", "Shows every tab again and puts them back in the game's order. Keybinds are kept.", "Puts every tab back on the wheel, in tab bar order.", "Removes the quick-access key from every tab.", "Show, hide and reorder the tabs on the bar.", "Choose which tabs go on the radial menu.", "TAB KEYS WORK IN RAID", "Lets a tab's key open the inventory in raid. Off = in raid, the keys only work while the inventory is open." } },
            { "ru", new[] { "СБРОСИТЬ", "ОСТАВЬТЕ ХОТЯ БЫ ОДНУ", "МОД", "ПАНЕЛЬ", "КОЛЕСО", "СЛОТЫ", "НЕ НА КОЛЕСЕ", "СБРОСИТЬ КЛАВИШИ", "Снова показывает все вкладки в порядке игры. Клавиши остаются.", "Возвращает все вкладки на колесо в порядке панели.", "Убирает клавишу быстрого доступа у всех вкладок.", "Показать, скрыть и упорядочить вкладки на панели.", "Выбрать, какие вкладки будут в радиальном меню.", "КЛАВИШИ ВКЛАДОК В РЕЙДЕ", "Клавиши вкладок открывают инвентарь в рейде. Выкл. = в рейде клавиши работают только при открытом инвентаре." } },
            { "ge", new[] { "ZURÜCKSETZEN", "EINE MUSS BLEIBEN", "MOD", "LEISTE", "RAD", "PLÄTZE", "NICHT IM RAD", "TASTEN LÖSCHEN", "Zeigt alle Tabs wieder in der Reihenfolge des Spiels an. Tasten bleiben.", "Setzt alle Tabs wieder aufs Rad, in Leistenreihenfolge.", "Entfernt die Schnelltaste von allen Tabs.", "Tabs auf der Leiste ein-, ausblenden und sortieren.", "Wählen, welche Tabs ins Radialmenü kommen.", "TAB-TASTEN IM RAID", "Tab-Tasten öffnen das Inventar auch im Raid. Aus = im Raid nur bei offenem Inventar." } },
            { "fr", new[] { "RÉINITIALISER", "GARDEZ-EN UN VISIBLE", "MOD", "BARRE", "ROUE", "EMPL.", "HORS DE LA ROUE", "EFFACER LES TOUCHES", "Réaffiche tous les onglets dans l'ordre du jeu. Les touches sont conservées.", "Remet tous les onglets sur la roue, dans l'ordre de la barre.", "Retire la touche rapide de tous les onglets.", "Afficher, masquer et trier les onglets de la barre.", "Choisir les onglets du menu radial.", "TOUCHES D'ONGLET EN RAID", "Les touches d'onglet ouvrent l'inventaire en raid. Désactivé = en raid, seulement avec l'inventaire ouvert." } },
            { "es", new[] { "RESTABLECER", "DEJA UNA VISIBLE", "MOD", "BARRA", "RUEDA", "HUECOS", "FUERA DE LA RUEDA", "BORRAR TECLAS", "Vuelve a mostrar todas las pestañas en el orden del juego. Las teclas se mantienen.", "Vuelve a poner todas las pestañas en la rueda, en el orden de la barra.", "Quita la tecla rápida de todas las pestañas.", "Mostrar, ocultar y ordenar las pestañas de la barra.", "Elegir qué pestañas van en el menú radial.", "TECLAS DE PESTAÑA EN INCURSIÓN", "Las teclas abren el inventario en incursión. Apagado = en incursión solo funcionan con el inventario abierto." } },
            { "es-mx", new[] { "RESTABLECER", "DEJA UNA VISIBLE", "MOD", "BARRA", "RUEDA", "HUECOS", "FUERA DE LA RUEDA", "BORRAR TECLAS", "Vuelve a mostrar todas las pestañas en el orden del juego. Las teclas se mantienen.", "Vuelve a poner todas las pestañas en la rueda, en el orden de la barra.", "Quita la tecla rápida de todas las pestañas.", "Mostrar, ocultar y ordenar las pestañas de la barra.", "Elegir qué pestañas van en el menú radial.", "TECLAS DE PESTAÑA EN INCURSIÓN", "Las teclas abren el inventario en incursión. Apagado = en incursión solo funcionan con el inventario abierto." } },
            { "it", new[] { "RIPRISTINA", "LASCIANE UNA VISIBILE", "MOD", "BARRA", "RUOTA", "SLOT", "FUORI DALLA RUOTA", "AZZERA TASTI", "Mostra di nuovo tutte le schede nell'ordine del gioco. I tasti restano.", "Rimette tutte le schede sulla ruota, nell'ordine della barra.", "Rimuove il tasto rapido da tutte le schede.", "Mostra, nascondi e riordina le schede della barra.", "Scegli quali schede vanno nel menu radiale.", "TASTI SCHEDE IN RAID", "I tasti delle schede aprono l'inventario in raid. Spento = in raid solo a inventario aperto." } },
            { "pl", new[] { "PRZYWRÓĆ DOMYŚLNE", "ZOSTAW JEDNĄ WIDOCZNĄ", "MOD", "PASEK", "KOŁO", "MIEJSCA", "POZA KOŁEM", "USUŃ SKRÓTY", "Pokazuje wszystkie karty w kolejności z gry. Skróty zostają.", "Przywraca wszystkie karty na koło w kolejności paska.", "Usuwa skrót klawiszowy ze wszystkich kart.", "Pokaż, ukryj i ułóż karty na pasku.", "Wybierz, które karty są w menu kołowym.", "SKRÓTY KART W RAJDZIE", "Skróty kart otwierają ekwipunek w rajdzie. Wył. = w rajdzie działają tylko przy otwartym ekwipunku." } },
            { "cz", new[] { "OBNOVIT VÝCHOZÍ", "NECHTE JEDNU VIDITELNOU", "MOD", "LIŠTA", "KOLO", "SLOTY", "MIMO KOLO", "SMAZAT KLÁVESY", "Znovu zobrazí všechny karty v pořadí hry. Klávesy zůstanou.", "Vrátí všechny karty na kolo v pořadí lišty.", "Odebere rychlou klávesu ze všech karet.", "Zobrazit, skrýt a seřadit karty na liště.", "Vybrat, které karty budou v kruhovém menu.", "KLÁVESY KARET V RAIDU", "Klávesy karet otevřou inventář i v raidu. Vyp. = v raidu jen při otevřeném inventáři." } },
            { "sk", new[] { "OBNOVIŤ PREDVOLENÉ", "NECHAJTE JEDNU VIDITEĽNÚ", "MOD", "LIŠTA", "KOLESO", "SLOTY", "MIMO KOLESA", "ZMAZAŤ KLÁVESY", "Znova zobrazí všetky karty v poradí hry. Klávesy zostanú.", "Vráti všetky karty na koleso v poradí lišty.", "Odstráni rýchlu klávesu zo všetkých kariet.", "Zobraziť, skryť a zoradiť karty na lište.", "Vybrať, ktoré karty budú v kruhovom menu.", "KLÁVESY KARIET V RAIDE", "Klávesy kariet otvoria inventár aj v raide. Vyp. = v raide len pri otvorenom inventári." } },
            { "hu", new[] { "ALAPÉRTELMEZETT", "EGY MARADJON LÁTHATÓ", "MOD", "SÁV", "KERÉK", "HELYEK", "NINCS A KERÉKEN", "GOMBOK TÖRLÉSE", "Minden fület újra megjelenít a játék sorrendjében. A gombok maradnak.", "Minden fület visszatesz a kerékre, a sáv sorrendjében.", "Eltávolítja a gyorsgombot minden fülről.", "Fülek megjelenítése, elrejtése és rendezése a sávon.", "Válaszd ki, mely fülek kerüljenek a körmenübe.", "FÜLGOMBOK RAIDBEN", "A fülgombok raidben is megnyitják a felszerelést. Ki = raidben csak nyitott felszerelésnél működnek." } },
            { "ro", new[] { "RESETARE", "LASĂ UNA VIZIBILĂ", "MOD", "BARĂ", "ROATĂ", "LOCURI", "ÎN AFARA ROȚII", "ȘTERGE TASTELE", "Afișează din nou toate filele în ordinea jocului. Tastele rămân.", "Pune toate filele înapoi pe roată, în ordinea barei.", "Elimină tasta rapidă de la toate filele.", "Afișează, ascunde și ordonează filele de pe bară.", "Alege ce file apar în meniul radial.", "TASTE FILE ÎN RAID", "Tastele filelor deschid inventarul și în raid. Oprit = în raid merg doar cu inventarul deschis." } },
            { "po", new[] { "REDEFINIR", "DEIXE UMA VISÍVEL", "MOD", "BARRA", "RODA", "ESPAÇOS", "FORA DA RODA", "LIMPAR TECLAS", "Mostra todas as abas de novo na ordem do jogo. As teclas ficam.", "Coloca todas as abas de volta na roda, na ordem da barra.", "Remove a tecla rápida de todas as abas.", "Mostrar, ocultar e ordenar as abas da barra.", "Escolher quais abas vão no menu radial.", "TECLAS DAS ABAS NA INCURSÃO", "As teclas das abas abrem o inventário na incursão. Desligado = na incursão só com o inventário aberto." } },
            { "tu", new[] { "VARSAYILANA DÖN", "BİRİ GÖRÜNÜR KALMALI", "MOD", "ÇUBUK", "ÇARK", "YUVA", "ÇARKTA DEĞİL", "TUŞLARI SIFIRLA", "Tüm sekmeleri yeniden gösterir ve oyunun sırasına koyar. Tuşlar kalır.", "Tüm sekmeleri çubuk sırasıyla çarka geri koyar.", "Tüm sekmelerden kısayol tuşunu kaldırır.", "Çubuktaki sekmeleri göster, gizle ve sırala.", "Radyal menüde hangi sekmelerin olacağını seç.", "BASKINDA SEKME TUŞLARI", "Sekme tuşları baskında envanteri açar. Kapalı = baskında sadece envanter açıkken çalışır." } },
            { "jp", new[] { "初期設定に戻す", "1つは表示が必要です", "MOD", "バー", "ホイール", "スロット", "ホイール外", "キー設定をリセット", "すべてのタブを再表示し、ゲームの順番に戻します。キーはそのままです。", "すべてのタブをバーの順番でホイールに戻します。", "すべてのタブからショートカットキーを外します。", "バーのタブを表示・非表示・並べ替えします。", "ラジアルメニューに入れるタブを選びます。", "レイド中もタブキーを使う", "レイド中もタブキーでインベントリを開けます。オフ＝レイド中はインベントリを開いている時のみ。" } },
            { "kr", new[] { "기본값으로 초기화", "하나는 표시해야 합니다", "모드", "바", "휠", "슬롯", "휠에 없음", "단축키 초기화", "모든 탭을 다시 표시하고 게임 순서로 되돌립니다. 단축키는 유지됩니다.", "모든 탭을 바 순서대로 휠에 다시 넣습니다.", "모든 탭의 단축키를 지웁니다.", "바의 탭을 표시, 숨기기, 정렬합니다.", "원형 메뉴에 넣을 탭을 고릅니다.", "레이드 중 탭 단축키", "레이드 중에도 탭 단축키로 인벤토리를 엽니다. 끄면 레이드 중에는 인벤토리가 열려 있을 때만 작동합니다." } },
            { "ch", new[] { "恢复默认", "至少保留一个", "模组", "标签栏", "轮盘", "槽位", "未在轮盘上", "重置按键", "重新显示所有标签页，并恢复游戏默认顺序。按键保留不变。", "按标签栏顺序把所有标签页放回轮盘。", "移除所有标签页的快捷键。", "显示、隐藏并排序标签栏上的标签页。", "选择哪些标签页放在轮盘菜单上。", "战局中启用标签快捷键", "战局中也可用标签快捷键打开物品栏。关闭＝战局中仅在物品栏打开时可用。" } }
        };

        internal static string Get(int key)
        {
            string culture = null;

            try
            {
                culture = LocalizationManager.Instance?.Culture;
            }
            catch (Exception)
            {
            }

            if (culture == null || !Languages.TryGetValue(culture, out string[] strings))
            {
                strings = Languages["en"];
            }

            return strings[key];
        }

        internal static Action OnLanguageChanged(Action callback)
        {
            try
            {
                return LocalizationManager.Instance?.AddLocaleUpdateListener(callback);
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
