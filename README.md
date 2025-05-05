# Šachový AI Engine s Unity GUI

Tento projekt je implementací šachové umělé inteligence v jazyce C# s vizuálním rozhraním vytvořeným v Unity. Hráč si může zahrát buď proti AI, nebo bez ní, případně použít režim analýzy pro získání doporučených tahů.

## Funkce AI

AI engine (`/AI-Engine/`) využívá:
- Bitboardy pro reprezentaci šachovnice
- Minimax s alpha-beta pruningem
- Move ordering pro zrychlení vyhledávání
- Iterative deepening pro dynamické řízení hloubky (Také zrychluje vyhledávání kvůli Move orderingu)

Samotná AI je vytvořena jako samostatný konzolový projekt a nemá žádné grafické rozhraní. Lze ji však snadno integrovat do jiných projektů (např. Unity).

## Unity GUI

Unity projekt (`/UnityGUI/`) obsahuje přehledné grafické rozhraní, kde lze využít AI engine. Po spuštění hlavního menu máte na výběr z několika režimů:

- **Play Offline** – Lokální hra dvou hráčů bez AI
- **Play Against AI** – Hra proti AI, během hry můžete přehodit stranu (barvu), za kterou hraje AI
- **Analysis** – Provedete vlastní tahy a po stisknutí tlačítka získáte:
  - Nejlepší tah podle AI
  - Hodnocení pozice (např. `+1.25`)
- **Quit** – Ukončí aplikaci

Z každého režimu se můžete kdykoliv vrátit do hlavního menu.

## Jak spustit projekt

### 1. Spuštění AI samostatně (pro vývoj nebo testování)
- Otevřete složku (`/AI-Engine/`) ve Visual Studiu
- Spusťte projekt (konzolová aplikace)
- Funguje pro testování nebo jako back-end

### 2. Spuštění Unity GUI
- Stáhněte si build (`/AI-Engine/UnityGUI/ChessBuild`)
- Spusťte .exe soubor
- V Main menu si vyberte režim
- Hrajte nebo analyzujte dle výběru

## Technologie

- C# (.NET)
- Unity (2022.3.43f1)
- Bitboardy, Minimax, Alpha-Beta Pruning

## Licence

Projekt je uvolněn pod licencí MIT. Volně k použití a úpravám.

## Odkazy

- [Repozitář na GitHubu](https://github.com/L1ghtninged/ChessAI/)
- QR kód s odkazem najdete na plakátu

## Autor

Jméno: David Tesař  
Třída: C3a

