# Guide de maintenance et d'évolution — Point à Canon

Ce document sert de **référence complète** pour modifier le jeu en toute sécurité (UI, logique métier, base de données, persistance).

---

## 1) Résumé du projet

- Stack: **C# WinForms (.NET)** + **PostgreSQL** via `Npgsql`
- Type de jeu: 2 joueurs, placement de points + tir canon
- Mode de victoire implémenté: score par **lignes valides de 5 points**
- Persistance: parties (`games`) + coups (`moves`)

Entrée application:
- `Program.cs` démarre `MainMenuForm`

---

## 2) Architecture réelle du code

## 2.1 Présentation (Forms)

- `Forms/MainMenuForm.cs`
  - Accueil: nouvelle partie / charger partie
  - Instancie une seule `DatabaseService` partagée

- `Forms/GameSetupForm.cs`
  - Configure grille (width/height) + couleurs joueurs
  - Crée un objet `Game` initial puis ouvre `GameForm`

- `Forms/LoadGameForm.cs`
  - Liste les parties (`GetAllGames`)
  - Charge une partie existante (ouvre `GameForm`)
  - Supprime une partie (`DeleteGame`)

- `Forms/GameForm.cs`
  - Écran principal de jeu
  - Gère:
    - rendu grille/points/lignes/canons
    - tours et score
    - mode pose / mode tir
    - animation balle (`System.Windows.Forms.Timer`)
    - sauvegarde

- `Forms/UiTheme.cs`
  - Thème visuel centralisé (couleurs, fonts, style boutons, cartes)

## 2.2 Logique métier

- `Services/GameLogic.cs`
  - Grille interne `int[,] grid`
  - Validation des coups (`IsValidMove`, `PlaceMove`)
  - Détection lignes gagnantes (`CheckWin`)
  - Contraintes avancées:
    - pas de ligne qui partage >1 point avec une ligne existante
    - pas de croisement diagonal sur ligne adverse
  - Suppression point au tir (`RemovePoint`) avec protections:
    - impossible supprimer son propre point
    - impossible supprimer un point déjà dans une ligne validée

## 2.3 Données / Persistance

- `Models/Game.cs`
  - Config + état partie + scores
- `Models/Move.cs`
  - Historique des coups (x, y, joueur, ordre)
- `Services/DatabaseService.cs`
  - CRUD parties + coups
  - `SaveMovesBulk` réécrit tous les coups d'une partie
  - `UpdateGameState` persiste tour + score

---

## 3) Schéma base de données

Script: `bdd/db_setup.sql`

Tables:

1. `games`
- `id` (PK)
- `grid_width`, `grid_height`
- `player1_color`, `player2_color`
- `current_turn`
- `status`
- `player1_score`, `player2_score`

2. `moves`
- `id` (PK)
- `game_id` (FK vers games, `ON DELETE CASCADE`)
- `x`, `y`
- `player_number`
- `move_order`

Important:
- `DatabaseService` exécute `EnsureColumnsExist()` au démarrage pour garantir les colonnes de score.
- La connection string est en dur dans `DatabaseService` (à externaliser si besoin).

---

## 4) Flux fonctionnel principal

## 4.1 Nouvelle partie

1. `MainMenuForm` -> `GameSetupForm`
2. L'utilisateur choisit dimensions/couleurs
3. `GameSetupForm` construit `Game` (id=0, scores=0, turn=1)
4. Ouverture de `GameForm`

## 4.2 Chargement partie

1. `MainMenuForm` -> `LoadGameForm`
2. Liste depuis `dbService.GetAllGames()`
3. Sélection -> `GameForm(game, dbService)`
4. `GameForm` charge `moves` et reconstruit les lignes via `ReplayAndGetWinLines`

## 4.3 Sauvegarde partie

Dans `GameForm` -> bouton Sauvegarder:

1. Si `game.Id == 0`, `CreateGame`
2. `SaveMovesBulk(game.Id, moves)`
3. `UpdateGameState(game.Id, game.CurrentTurn, score1, score2)`

---

## 5) Règles métier implémentées (état actuel)

## 5.1 Placement

- Un point est valide si:
  - coordonnées dans bornes
  - case vide
- Après pose:
  - si ligne(s) valide(s): score +1 par ligne trouvée, joueur rejoue
  - sinon: tour passe à l'adversaire

## 5.2 Détection de ligne

`CheckWin(x, y, player)`:

- directions testées: horizontal, vertical, diagonale montante, diagonale descendante
- construit la ligne continue autour du point posé
- si longueur >=5:
  - recherche un sous-segment de 5 contenant le point posé
  - vérifie contraintes:
    - pas croiser ligne adverse (`DoesLineCrossOpponent`)
    - pas partager >1 point avec une ligne existante (`SharesMoreThanOnePointWithAnyExistingLine`)
  - prend au maximum **1 ligne par direction**

## 5.3 Tir canon

État tir dans `GameForm`:

- mode `Shoot`
- puissance 1..9 via `Ctrl + 1..9`
- déplacement canon via flèches haut/bas
- positionnement canon via clic dans marge du côté du joueur
- tir horizontal uniquement (`FireBall` + timer)

Règle de puissance:
- `targetCells = Round(shotPower * GridWidth / 9, AwayFromZero)`
- `ballMaxDist = targetCells * cellSize`

Effets collision:
- détecte intersections de la ligne parcourue
- si point ennemi destructible touché:
  - suppression du point (`RemovePoint`)
  - suppression du move correspondant
  - recalcul `MoveOrder`
  - tireur garde le tour
- sinon: tour passe à l'adversaire

---

## 6) UI/Rendering: où modifier quoi

## 6.1 Thème global

Fichier: `Forms/UiTheme.cs`

Modifier ici pour impacter tout le jeu:
- couleurs globales (`AppBackground`, `CardBackground`, `Accent`, etc.)
- typographies (`TitleFont`, `BodyFont`)
- style boutons (`StylePrimaryButton`, `StyleSecondaryButton`)
- style cartes (`CreateCard`)

## 6.2 Écrans menu/config/load

- `MainMenuForm.cs`: structure du menu principal
- `GameSetupForm.cs`: champs de configuration
- `LoadGameForm.cs`: liste des parties + actions charger/supprimer

## 6.3 Plateau de jeu

Fichier: `Forms/GameForm.cs`

Points d'entrée importants:
- `GridPanel_Paint` -> rendu grille, points, lignes, canons, balle
- `DrawCannon` -> forme/taille des canons
- constantes de layout:
  - `cellSize`
  - `pointRadius`
  - `cannonMargin`

---

## 7) Dossier “modifs fréquentes” (recettes rapides)

## 7.1 Changer le nombre de points requis (actuellement 5)

Dans `GameLogic.CheckWin`:
- remplacer la constante logique 5 (tests `Count >= 5`, `GetRange(i, 5)`, etc.)
- vérifier les impacts sur:
  - score
  - validation de partage/croisement
  - rendu des lignes

Recommandation:
- extraire une constante `RequiredAlignedPoints` pour éviter les oublis.

## 7.2 Autoriser/interdire de nouvelles règles de croisement

- Méthodes concernées:
  - `DoesLineCrossOpponent`
  - `IsSegmentInOpponentLine`

## 7.3 Modifier logique de tir

- `FireBall` (distance, vitesse initiale)
- `BallTimer_Tick` (collision, fin de tir)
- `EndShot` (gestion du tour)

## 7.4 Ajouter les noms de joueurs

Étapes:
1. Ajouter colonnes DB (`games.player1_name`, `games.player2_name`)
2. Mettre à jour `Game.cs`
3. Mettre à jour `CreateGame`, `GetGame`, `GetAllGames`
4. Ajouter champs UI dans `GameSetupForm`
5. Afficher noms dans `GameForm` (titre/HUD)

## 7.5 Changer le comportement de sauvegarde

Actuel:
- `SaveMovesBulk` supprime puis réinsère tous les coups

Alternative possible:
- sauvegarde incrémentale (INSERT seulement nouveaux coups)
- utile pour grosses parties

---

## 8) Points d'attention (pièges actuels)

1. **Bornes Y au clic dans `GameForm`**
- Le clamp utilise `Math.Min(gy, game.GridHeight)` (et non `GridHeight - 1`)
- Peut placer le canon une ligne hors grille visuelle
- Recommandé: borner à `GridHeight - 1`

2. **Suppression de coups après tir**
- `moves.RemoveAll(m => m.X == gx && m.Y == ballRow)`
- Si plusieurs entrées historiques existent au même point (cas anormal), toutes seront supprimées

3. **Connection string hardcodée**
- `DatabaseService` contient user/password en dur
- Recommandé: config externe (json/env vars)

4. **Exceptions DB masquées dans `EnsureColumnsExist`**
- Le `catch {}` silencieux masque les erreurs de schéma
- Recommandé: logger au moins le message d'erreur

---

## 9) Checklist de non-régression après modification

Après une modif, tester au minimum:

- lancement app depuis menu principal
- nouvelle partie (plusieurs tailles de grille)
- placement point valide/invalide
- détection ligne et score
- changement de tour normal
- mode tir + puissance + collision
- tir qui touche (garder tour) vs tir raté (passer tour)
- sauvegarder puis recharger une partie
- suppression d'une partie depuis Load

Commandes utiles:
- `dotnet restore PointGame.csproj`
- `dotnet build PointGame.csproj -c Debug`
- `dotnet run --project PointGame.csproj`

---

## 10) Plan d'amélioration conseillé (progressif)

Priorité haute:
1. Externaliser config DB
2. Corriger borne Y canon (`GridHeight - 1`)
3. Introduire constantes de règles (points alignés, puissance max)

Priorité moyenne:
4. Séparer davantage UI / logique de jeu (testabilité)
5. Ajouter logs techniques (DB + erreurs gameplay)

Priorité confort:
6. Ajouter tests unitaires sur `GameLogic`
7. Ajouter export/import de partie (JSON)

---

## 11) Références code rapides

- Entrée app: `Program.cs`
- UI accueil: `Forms/MainMenuForm.cs`
- UI setup: `Forms/GameSetupForm.cs`
- UI chargement: `Forms/LoadGameForm.cs`
- UI jeu + rendu + tir: `Forms/GameForm.cs`
- thème UI: `Forms/UiTheme.cs`
- logique métier: `Services/GameLogic.cs`
- accès DB: `Services/DatabaseService.cs`
- modèles: `Models/Game.cs`, `Models/Move.cs`
- SQL init: `bdd/db_setup.sql`

---

## 12) Convention proposée pour les futures modifs

- Toute nouvelle règle de jeu doit être centralisée dans `GameLogic`.
- Toute nouvelle donnée persistée doit:
  1. exister en DB,
  2. exister dans `Models`,
  3. être lue/écrite par `DatabaseService`,
  4. être reflétée en UI.
- Toute modification visuelle globale passe d'abord par `UiTheme`.
- Toute modif sensible doit passer par la checklist section 9 avant livraison.

---

Dernière mise à jour: 2026-03-25