# Guide Joueur — Point à Canon

Ce document explique **comment jouer** rapidement: règles principales, touches clavier, actions possibles, sauvegarde/chargement.

---

## 1) Objectif

- Jeu à 2 joueurs.
- À ton tour, tu peux:
  - **poser un point**, ou
  - **tirer avec le canon**.
- Tu marques des points en formant des **lignes valides de 5 points**.
- La partie continue, on compare les scores.

---

## 2) Démarrer une partie

1. Ouvrir l’application.
2. Cliquer sur **Nouvelle partie**.
3. Choisir:
   - largeur de grille,
   - hauteur de grille,
   - couleur joueur 1,
   - couleur joueur 2.
4. Cliquer sur **Démarrer**.

---

## 3) Écran de jeu (vue rapide)

- **Plateau**: grille de jeu.
- **Canons**:
  - Joueur 1 à gauche,
  - Joueur 2 à droite.
- **Boutons**:
  - `Sauvegarder`
  - `Mode Tir` / `Mode Pose`
  - `Tirer` (actif seulement en mode tir avec puissance choisie)
- **Statut**: indique le mode, le joueur courant, la ligne du canon, la puissance.

---

## 4) Contrôles (touches)

## 4.1 Clavier

- `Flèche Haut` : monter le canon du joueur actif.
- `Flèche Bas` : descendre le canon du joueur actif.
- `Ctrl + 1` à `Ctrl + 9` : choisir la puissance de tir (mode tir).

## 4.2 Souris

- **Mode Pose**: cliquer sur une intersection libre pour poser un point.
- **Mode Tir**: cliquer dans la marge du canon (côté du joueur actif) pour placer le canon sur une autre ligne.
- **Boutons UI**: cliquer pour changer de mode, tirer, sauvegarder.

---

## 5) Déroulement d’un tour

## Option A — Poser un point

1. Être en `Mode Pose`.
2. Cliquer une intersection libre.
3. Si une ligne valide est créée:
   - tu gagnes 1 point (ou plus si plusieurs lignes valides détectées),
   - tu rejoues.
4. Sinon, le tour passe à l’adversaire.

## Option B — Tirer au canon

1. Passer en `Mode Tir`.
2. Positionner le canon (flèches ou clic sur marge).
3. Choisir la puissance avec `Ctrl + 1..9`.
4. Cliquer `Tirer`.
5. Résultat:
   - si la balle détruit un point adverse: tu rejoues,
   - sinon: le tour passe à l’adversaire.

---

## 6) Règles importantes

- Impossible de poser un point sur une case déjà occupée.
- Impossible de détruire ses propres points.
- Impossible de détruire un point déjà inclus dans une ligne validée.
- Une ligne validée respecte la logique du jeu (alignement de 5 + contraintes internes).

---

## 7) Puissance du tir (simple)

- La puissance va de **1 à 9**.
- Plus la puissance est haute, plus la balle va loin horizontalement.
- La distance est adaptée à la taille de la grille (règle proportionnelle).

---

## 8) Sauvegarder / Charger / Supprimer

## Sauvegarder

- En jeu, cliquer sur `Sauvegarder`.
- La partie (coups, score, tour) est enregistrée en base PostgreSQL.

## Charger

1. Menu principal -> `Charger une partie`.
2. Sélectionner la partie.
3. Cliquer `Charger`.

## Supprimer

1. Menu principal -> `Charger une partie`.
2. Sélectionner la partie.
3. Cliquer `Supprimer` puis confirmer.

---

## 9) Conseils pratiques

- En mode tir, vérifie toujours:
  - la ligne du canon,
  - la puissance affichée,
  - le joueur actif.
- Sauvegarde régulièrement, surtout après un bon score.
- Si `Tirer` est grisé, choisis d’abord une puissance (`Ctrl + 1..9`).

---

## 10) Résumé ultra-court

- `Mode Pose` -> clic intersection.
- `Mode Tir` -> placer canon + `Ctrl + 1..9` + `Tirer`.
- `Haut/Bas` -> bouger canon.
- `Sauvegarder` -> enregistrer la partie.

---

Dernière mise à jour: 2026-03-25