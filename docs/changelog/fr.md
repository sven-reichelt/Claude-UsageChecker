# Journal des modifications

Traduction de [CHANGELOG.md](../../CHANGELOG.md). L'anglais est la source ; en
cas de divergence, c'est le texte anglais qui fait foi.

Le format suit [Keep a Changelog](https://keepachangelog.com/fr/1.1.0/), la
numérotation [Semantic Versioning](https://semver.org/lang/fr/).

## [Non publié]

## [0.7.2] – 2026-08-20

### Corrigé
- **Le résumé des nouveautés ne s'affichait pas à la sortie d'une version
  d'essai.** La dernière version exécutée était notée avec trois nombres et sans
  étiquette : 0.7.1-beta.5 et la 0.7.1 finale laissaient donc la même trace, le
  pas entre les deux était invisible et le résumé ne venait jamais.
  L'étiquette est désormais enregistrée, et atteindre la version finale compte
  comme un pas en avant même si le numéro ne bouge pas. Entre deux versions
  d'essai du même numéro, rien ne s'affiche : l'historique n'a rien de nouveau à
  y dire.
- **Le résumé indique qu'une version d'essai est en cours.** Le titre nomme la
  version de l'entrée de l'historique, et l'historique ne connaît pas les
  versions d'essai : 0.7.2-beta.1 se lisait donc « Nouveautés de la version
  0.7.2 » sans que rien n'indique que la version finale n'était pas encore
  atteinte.
## [0.7.1] – 2026-08-20

### Ajouté
- **Le bouton d'actualisation cherche au passage une nouvelle version.** Un
  clic au lieu de deux. Désactivable dans les paramètres, pour qui préfère ne
  pas solliciter le réseau plus que nécessaire.
- **Les paramètres se présentent sur deux colonnes.** Sur une seule, la fenêtre
  devenait haute et étroite : il fallait défiler sur un écran de portable pour
  des réglages qui tiennent confortablement côte à côte.

### Modifié
- Préparation des versions d'essai. Rien n'en est visible à l'usage.

### Corrigé
- **Le menu de la zone de notification s'adapte à son contenu.** Sa largeur
  était figée et la ligne de l'utilisation supplémentaire n'y tenait pas :
  elle porte deux montants et une devise, elle passait donc à la ligne. Dans
  un menu dont chaque autre ligne est une limite, une ligne repliée se lit
  comme deux.
- **Corrections de traduction.** Six langues appelaient encore « crédits »
  l'utilisation supplémentaire alors que le chiffre est de l'argent, et toutes
  affirmaient qu'un historique non traduit s'affiche en allemand – il
  s'affiche en anglais. Les messages d'erreur du gestionnaire d'informations
  d'identification de Windows étaient figés en allemand ; ils suivent
  désormais la langue de l'interface.

### Sécurité
- La page de publication issue de la réponse de GitHub est désormais soumise
  aux mêmes exigences que les adresses de téléchargement : https uniquement.
- Les actions des workflows sont épinglées à des empreintes de commit plutôt
  qu'à des étiquettes mobiles. Une étiquette peut être redirigée par qui
  administre le dépôt de l'action ; une empreinte non. Cela compte surtout
  pour l'action de publication tierce, qui s'exécute avec droit d'écriture
  dans le workflow qui compile l'exécutable publié.

## [0.7.0] – 2026-08-20

### Modifié
- **Le menu de la zone de notification est désormais dessiné par
  l'application.** Windows dessine les menus contextuels dans la police du
  système, sans cadre ; à côté des fenêtres de cette application, cela
  ressemblait à un autre programme. Il porte maintenant le même cadre, la même
  police et les mêmes espacements.
- **Le menu indique la version.** L'entrée s'appelle désormais « À propos de
  Claude UsageChecker 0.7.0 … ». C'est la première chose que l'on demande à
  qui signale un problème.

## [0.6.4] – 2026-08-20

### Corrigé
- **Une fenêtre dont la réinitialisation était échue n'avait aucune phrase
  correcte.** Le bloc prévu pour une durée était inséré dans un emplacement
  qui en attend une, ce qui donnait « Session : 39 % - encore maintenant ».
  Les quatre endroits qui parlent d'un temps restant disposent désormais
  d'une phrase propre : « réinitialisation attendue ».

## [0.6.3] – 2026-08-20

### Corrigé
- **La fenêtre de détails se plaçait sous le milieu de l'écran dès qu'une mise
  à jour était disponible.** Elle est créée une fois puis réutilisée, si bien
  que `CenterScreen` n'agissait qu'à la première ouverture ; l'avis de mise à
  jour arrive quelques secondes plus tard et la grandit d'une centaine de
  pixels, vers le bas. Elle est désormais recentrée à chaque changement de
  taille.

## [0.6.2] – 2026-08-20

### Modifié
- **L'icône de la zone de notification indique son état.** Déconnecté : gris.
  Connecté et tout va bien : une coche verte ; au seuil d'avertissement, un
  point d'interrogation ambre ; au seuil critique, un point d'exclamation
  rouge. Auparavant la couleur s'en chargeait seule. Un seul signe par état :
  à seize pixels, deux ne se distinguent plus.

### Corrigé
- **Le crédit supplémentaire était affiché cent fois trop grand et dans la
  mauvaise unité.** L'API indique `used_credits: 2276`, et ce ne sont pas 2276
  crédits mais 22,76 EUR : un montant dans la plus petite unité de sa monnaie.
  L'application prenait le nombre au pied de la lettre. **La monnaie vient du
  compte** — USD, BRL, selon le cas —, tout comme le nombre de décimales, car
  toutes les monnaies n'en ont pas deux. Le champ `spend`, qui dit ce que ses
  chiffres signifient, est désormais lu en priorité.

## [0.6.1] – 2026-08-20

### Modifié
- Version de maintenance.

## [0.6.0] – 2026-08-20

### Corrigé
- **Les limites hebdomadaires propres à un modèle n'apparaissaient pas.** Qui
  dispose d'une limite Fable ne la voyait nulle part – ni dans l'infobulle, ni
  dans le menu contextuel, ni dans la fenêtre de détails – alors que Claude
  lui-même l'indique. La cause : l'application lisait les champs
  `seven_day_opus` et `seven_day_sonnet`, qui portent le nom du modèle dans
  l'identifiant. Les deux sont désormais vides, et il n'existe pas de champ
  `seven_day_fable`.

  L'API fournit les mêmes valeurs dans une liste `limits`, qui nomme le modèle
  dans son contenu (`scope.model.display_name`). Cette liste est désormais lue
  en priorité ; les anciens champs restent en secours. **Tout modèle futur
  apparaîtra de lui-même**, sans modification ici. Détails dans
  [docs/api-research.md](../api-research.md).

  L'icône de la zone de notification tient également compte de ces limites –
  auparavant elle restait verte alors qu'un quota de modèle était déjà épuisé.

### Ajouté
- **Neuf langues.** Allemand, anglais, espagnol, français, italien, portugais
  (Brésil et Portugal séparément), russe et chinois simplifié. Au premier
  démarrage, l'application suit la langue du système ; elle se change dans la
  fenêtre d'installation – où le choix prend effet immédiatement et où **les
  deux** boutons le reprennent – puis à tout moment dans les paramètres.

  La culture des nombres, des dates et des heures change avec la langue : qui
  passe l'interface en français n'y attend pas des dates allemandes.

  **Le journal des modifications est traduit lui aussi.** Le récapitulatif
  affiché après une mise à jour paraît donc dans la même langue que
  l'interface. L'anglais est la source et figure dans
  [CHANGELOG.md](../../CHANGELOG.md) ; les traductions, dont l'allemand, se
  trouvent sous [docs/changelog/](.).

  Les noms de produits et de modèles ne sont pas traduits : « Claude
  UsageChecker », « Claude Code » et le nom du modèle fourni par l'API –
  « Fable » se dit Fable dans toutes les langues.
- **Les seuils d'avertissement et critique sont configurables.** Le taux
  d'utilisation à partir duquel l'icône passe au jaune, puis au rouge, se règle
  désormais dans les paramètres au lieu d'être figé dans le code (valeurs par
  défaut inchangées : 75 % et 90 %). Un seuil d'avertissement supérieur au seuil
  critique est refusé plutôt que corrigé en silence – il ne se déclencherait
  jamais.
- **Un récapitulatif des nouveautés après une mise à jour.** Au premier
  démarrage d'une nouvelle version, l'application montre ce qui a changé depuis
  la version précédemment exécutée. Les versions intermédiaires sautées sont
  incluses. La source est le journal livré avec le programme, sans accès
  réseau – le récapitulatif est donc disponible hors ligne et montre
  nécessairement l'état correspondant à la version en cours. Il est omis au tout
  premier démarrage.
- **« À propos de Claude UsageChecker » dans le menu contextuel.** Affiche
  l'icône, la version, une courte description et mène à la page du projet. Le
  journal complet y est également accessible.

### Modifié
- **La langue du projet est l'anglais.** Documentation, commentaires,
  identifiants et noms de tests – tout dans le dépôt sauf les textes allemands
  de l'interface et l'historique des commits jusqu'ici. La raison est simple :
  c'est un dépôt public, et quiconque le trouve devrait pouvoir le lire. La
  documentation en allemand est maintenue en parallèle sous [docs/de/](../de/).
- La version exécutée en dernier est consignée dans le fichier de paramètres
  (`lastRunVersion`). C'est la seule indication permettant à l'application de
  reconnaître une mise à jour – l'exécutable lui-même ignore ce qui tournait
  avant lui.

  Les versions antérieures ne connaissaient pas ce champ. Qui met à jour depuis
  l'une d'elles n'a donc rien de consigné – dans ce cas, c'est la présence du
  fichier de paramètres qui tranche : elle prouve que l'application a déjà
  tourné, et les nouveautés de la version en cours sont affichées. Sans cette
  branche, la version qui introduit le récapitulatif serait justement celle qui
  n'en montrerait aucun.
- `MonitorOptions` ne porte plus les seuils. Le moniteur ne les a jamais lus –
  il récupère des valeurs, il ne les juge pas. Le jugement a lieu à un seul
  endroit, dans `TrayIconSeverityResolver`, à partir des paramètres
  utilisateur. Deux endroits pour la même valeur seraient une invitation à
  tourner plus tard le mauvais bouton.
- Le `PollInterval` calculé n'est plus écrit dans le fichier de paramètres. Il
  n'y était jamais lu ; il ressemblait seulement à une deuxième indication de
  l'intervalle de relevé, susceptible de contredire la première.
- **La fenêtre des paramètres reste sur l'écran.** Elle grandit avec son contenu
  et n'est pas redimensionnable ; sur un écran peu haut, elle dépassait par le
  bas en emportant le bouton « Enregistrer ». Deux garde-fous désormais : la
  rangée de boutons est ancrée sous la zone de défilement et reste visible
  quelle que soit la hauteur de l'écran, et la fenêtre est mesurée une fois mise
  en page puis remontée si elle dépasse encore. Plafonner la hauteur ne
  suffisait pas : Avalonia centre une fenêtre d'après la hauteur qu'elle a à
  l'ouverture, et le contenu grandit ensuite.

### Supprimé
- **La saisie manuelle d'un jeton** a disparu des paramètres. Elle ne pouvait
  servir à personne : le seul jeton que l'on pouvait coller provient de
  `claude setup-token`, et il lui manque la portée `user:profile` exigée par le
  point d'accès. Les jetons qui fonctionnent – celui de l'installation Claude
  Code et celui de la connexion propre à l'application – ne se saisissent jamais
  à la main. Un jeton enregistré par une version antérieure continue d'être lu ;
  seule la façon d'en ajouter un a disparu. Justification dans
  [docs/api-research.md](../api-research.md).

### Documentation
- **Modèles pour les rapports d'erreur et les demandes de fonctionnalité** sous
  `.github/ISSUE_TEMPLATE/`, ainsi qu'un modèle de pull request et
  [CONTRIBUTING.md](../../CONTRIBUTING.md) – en anglais, afin qu'un signalement
  puisse aussi venir hors de l'espace germanophone. Les formulaires demandent la
  version, le système d'exploitation, l'abonnement et la source du jeton, et
  mettent explicitement en garde contre le collage d'un jeton.
- Les notes sur l'API ([docs/api-research.md](../api-research.md)) consignent
  le nouveau format de réponse – y compris les champs qui restent inutilisés, et
  pourquoi.

## [0.5.0] – 2026-08-19

### Modifié
- L'emplacement d'installation est désormais
  `%LOCALAPPDATA%\Programs\ClaudeUsageChecker` au lieu de
  `%USERPROFILE%\ClaudeUsageChecker`. C'est l'emplacement prévu par Windows pour
  les applications sans droits d'administrateur – VS Code et Signal s'y trouvent
  également. La racine du profil utilisateur reste ainsi libre, là où personne
  n'attend de programmes à côté des documents et des téléchargements.

  **Les installations existantes ne se déplacent pas d'elles-mêmes.** Elles
  continuent de tourner depuis l'ancien emplacement. Pour déménager, il suffit
  d'ouvrir les paramètres et d'enregistrer – si la case de démarrage automatique
  est cochée, la copie se fait vers le nouvel emplacement. L'ancien répertoire
  peut ensuite être supprimé à la main.

## [0.4.2] – 2026-08-19

### Corrigé
- Qui passait l'installation au premier démarrage puis cochait seulement
  « Démarrer avec Windows » obtenait une entrée de démarrage automatique
  pointant vers le dossier de téléchargement – sans valeur dès le premier
  nettoyage de ce dossier. La case entraîne désormais aussi le déménagement,
  avec une indication préalable du chemin cible et du redémarrage.
- **Décocher** la case laisse en revanche l'application où elle est. Seule
  l'entrée de démarrage automatique est supprimée ; une fois installée, elle le
  reste.

## [0.4.1] – 2026-08-19

### Corrigé
- Les dossiers d'extraction des versions antérieures restaient dans le
  répertoire temporaire. Un fichier unique compressé ne peut pas charger ses
  bibliothèques natives depuis le paquet – le runtime .NET les extrait vers
  `%TEMP%\.net\ClaudeUsageChecker\<identifiant>`, et comme l'identifiant dépend
  du contenu, chaque version obtenait son propre dossier. Environ 16 Mo par mise
  à jour, s'accumulant sans limite. L'application les nettoie désormais
  elle-même.

### Documentation
- [SECURITY.md](../../SECURITY.md) énumère intégralement ce que l'application
  stocke et où, ainsi que ce qui subsisterait après une désinstallation.

## [0.4.0] – 2026-08-19

### Ajouté
- **Installation permanente.** Si l'application s'exécute hors de son
  emplacement cible, elle propose une seule fois au premier démarrage de se
  copier vers `%USERPROFILE%\ClaudeUsageChecker`, de configurer le démarrage
  automatique et de redémarrer depuis là. La raison n'est pas le goût de
  l'ordre : le démarrage automatique, l'épinglage dans la zone de notification
  et la mise à jour automatique dépendent tous du chemin de l'exécutable – s'il
  se trouve dans le dossier de téléchargement, les trois se cassent dès que ce
  dossier est nettoyé.
- Le démarrage automatique est activé en même temps que l'installation et pointe
  vers le chemin cible, pas vers l'emplacement de départ. Désactivable dans les
  paramètres.

### Modifié
- La fenêtre de détails apparaît au centre de l'écran et porte une fine bordure
  de la couleur de l'icône au lieu du cadre système.

### Ajouté
- Un test vérifie que la bordure obtient réellement sa couleur. Une
  `DynamicResource` non résolue resterait sinon silencieusement vide.

## [0.3.3] – 2026-08-19

### Modifié
- Le fichier publié porte le même nom dans chaque version :
  `ClaudeUsageChecker.exe` au lieu de `ClaudeUsageChecker-0.3.2-win-x64.exe`.
  La mise à jour automatique écrit la nouvelle version au chemin du fichier en
  cours d'exécution – un nom versionné annoncerait ensuite une version erronée.
  Et Windows mémorise l'épinglage dans la zone de notification par chemin : si
  le nom ne restait pas identique, l'icône retournerait dans la zone de
  débordement après chaque mise à jour.

## [0.3.2] – 2026-08-19

### Corrigé
- Les boutons de l'avis de mise à jour dépassaient de la fenêtre. Côte à côte,
  ils réclamaient environ 420 pixels, la fenêtre en fait 380 – « Ouvrir la page
  de publication » n'était lisible qu'à moitié. Ils sont désormais l'un sous
  l'autre.

### Ajouté
- Des tests qui révèlent les débordements dans la fenêtre de détails. Ils
  mesurent le placement réel après un cycle de mise en page complet et comparent
  le bord droit de chaque élément à la largeur de la fenêtre. Ni la taille
  souhaitée des contrôles ni celle de la fenêtre ne conviennent pour cela :
  Avalonia limite les deux à la valeur indiquée, si bien qu'un débordement ne
  peut pas y apparaître.

## [0.3.1] – 2026-08-19

### Modifié
- L'interface écrit les trémas comme des trémas. On y lisait auparavant « Auf
  Aktualisierungen pruefen », « Gueltig bis » ou « Der Browser liess sich nicht
  oeffnen » – ces translittérations venaient du développement et n'avaient rien
  à faire à l'écran. 36 chaînes concernées.
- Le message signalant l'absence de droit d'accès renvoie désormais aux
  paramètres là où il réclamait auparavant un jeton.

### Ajouté
- Un test vérifie l'encodage des caractères depuis le fichier source jusqu'à
  l'interface. Une erreur d'encodage se manifeste ainsi lors des tests plutôt
  que chez l'utilisateur.

## [0.3.0] – 2026-08-19

La première version capable de se mettre à jour elle-même. À partir d'ici, un
clic suffit – le téléchargement manuel n'est plus nécessaire.

### Corrigé
- Les versions sont affichées avec trois composantes. La quatrième provient de
  la version d'assembly et ne signifie rien – « La version 0.2.0.0 est à jour »
  ne faisait qu'embrouiller.

### Ajouté
- **Mise à jour en un clic.** « Installer maintenant et redémarrer » télécharge
  la nouvelle version, vérifie sa somme SHA-256 par rapport à celle publiée,
  remplace le fichier en cours d'exécution et redémarre. Un avis qu'il faut
  traiter à la main finit en pratique par rester en plan.
  - Si la somme de contrôle ne correspond pas ou manque, rien n'est installé et
    rien n'est exécuté.
  - L'adresse provient de la réponse de GitHub concernant ce dépôt ; les
    adresses sans HTTPS sont écartées.
  - Uniquement après un clic explicite, jamais en silence en arrière-plan.
  - Le remplacement exploite le fait que Windows autorise le renommage d'un
    fichier en cours d'exécution. Si la mise en place échoue, le renommage est
    annulé.

### Modifié
- « Afficher les détails » a été retiré du menu contextuel. Un clic gauche sur
  l'icône ouvre la fenêtre de détails, et les chiffres figurent de toute façon
  dans les lignes d'état au-dessus – l'entrée ne proposait qu'une seconde fois
  le même chemin.
- Le message signalant l'absence de droit d'accès cite d'abord la connexion
  propre. On y lisait auparavant « Connecte-toi dans Claude Code » – un conseil
  que personne ne pouvait suivre sur une machine sans Claude Code.

## [0.2.0] – 2026-08-19

Première publication. Fichier unique autonome pour Windows x64, 21 Mo, aucun
runtime .NET requis.

### Affichage

- Limite de session de 5 heures et limites hebdomadaires (totale, Opus, Sonnet)
  depuis `GET /api/oauth/usage` – des valeurs faisant autorité, pas des
  estimations.
- Infobulle avec le taux d'utilisation, l'heure de réinitialisation et le temps
  restant. Si la réinitialisation tombe un autre jour, le jour de la semaine la
  précède ; à partir d'une semaine, la date – une simple heure serait ambiguë
  pour la limite hebdomadaire.
- Menu contextuel avec **toutes** les limites signalées.
- Fenêtre de détails avec barres de progression, heures de réinitialisation,
  crédits supplémentaires (`extra_usage`) et la source de jeton réellement
  utilisée.
- Icône de la zone de notification en couleurs : normale, tendue, critique.

### Connexion

- **Connexion propre via OAuth avec PKCE** (RFC 7636, S256) – rend l'application
  indépendante d'une installation de Claude Code en cours d'exécution. Le seul
  droit demandé est `user:profile` ; explicitement **pas** `user:inference` ni
  `org:create_api_key`.
- Sans serveur web local : le code est collé à la main au lieu d'être reçu par
  une redirection vers `localhost`. Aucun port ouvert.
- Le jeton propre est renouvelé automatiquement. Pour le jeton lu chez Claude
  Code, cela est délibérément omis – un jeton de rafraîchissement rotatif
  invaliderait sa connexion. Entrées séparées dans le stockage sécurisé.
- Si la connexion propre expire, elle est supprimée et signalée, au lieu de
  revenir silencieusement à Claude Code. Une simple perturbation (réseau, 5xx,
  limitation de débit) la laisse en revanche intacte.
- Chaîne de repli : connexion propre → jeton enregistré → variable
  d'environnement → Claude Code. Si l'API refuse une source, la requête passe à
  la suivante.

### Fonctionnement

- Intervalle de relevé d'au moins 180 secondes, temporisation exponentielle
  après les échecs, le `Retry-After` du serveur prime.
- Une seule instance par session ouverte.
- Démarrage automatique avec Windows, désactivable.
- Recherche de mises à jour via les publications GitHub. Rien n'est téléchargé
  ni exécuté – seulement signalé, et la page de publication ouverte sur demande.
- Les erreurs dans les actions de la zone de notification ne terminent plus
  l'application, mais atterrissent avec leur contexte dans `crash.log`.

### Constats qui ont façonné la conception

- **`claude setup-token` ne convient pas à cet usage.** De tels jetons sont
  valides et fonctionnent avec `/v1/messages`, mais ne portent pas
  `user:profile`. Le point de terminaison d'utilisation les refuse avec un
  HTTP 403. C'était l'hypothèse initiale du projet, et elle est réfutée.
- **Le point de terminaison des jetons se trouve sur `platform.claude.com`**, et
  non plus sur `console.anthropic.com` – là, il répond HTTP 404.
- **Le `User-Agent` est obligatoire.** Sans un user agent Claude Code, le point
  de terminaison d'utilisation limite durablement avec un HTTP 429.
- Compilé en mode réduit et compressé : 21 Mo au lieu de 93 Mo, démarrage en
  2,3 au lieu de 7,2 secondes, 87 au lieu de 136 Mo de mémoire. La réduction
  l'emporte sur les trois axes – le code supprimé n'a pas non plus besoin d'être
  chargé et compilé.

### Limitations connues

- Le paquet n'est **pas signé**. Windows SmartScreen signale un éditeur inconnu
  au premier démarrage.
- Combien de temps la connexion propre survit à une longue pause reste inconnu –
  Anthropic ne documente pas la durée de vie du jeton de rafraîchissement.
- Le processus de connexion utilise l'identifiant client OAuth publiquement
  connu de Claude Code, Anthropic ne proposant pas d'enregistrer ses propres
  applications. Ce n'est pas une voie officiellement prise en charge ; elle peut
  changer à tout moment.
- macOS est préparé, mais pas encore réalisé.
