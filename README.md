# EMGAS - Système de Gestion d'Inventaire de Voitures

## Description

EMGAS est une application web de gestion d'inventaire pour un concessionnaire automobile. Elle permet de suivre les voitures en stock, leurs détails, statuts de vente et réparations. L'application est développée en ASP.NET Core avec Razor Pages.

## Fonctionnalités principales

- **Gestion des voitures** : Ajouter, modifier et supprimer des véhicules
- **Suivi de l'inventaire** : Visualiser l'état actuel du stock automobile
- **Gestion des images** : Télécharger et gérer plusieurs photos par véhicule
- **Filtrage avancé** : Rechercher des voitures par marque, modèle, année, statut, etc.
- **Gestion des réparations** : Suivre les réparations effectuées sur les véhicules
- **Système d'authentification** : Différents niveaux d'accès (Administrateur, Manager, etc.)
- **Interface multilingue** : Application disponible en français

## Technologies utilisées

- **Framework** : ASP.NET Core 6.0
- **Base de données** : SQLite (EMGAS.db)
- **ORM** : Entity Framework Core
- **Front-end** : Bootstrap 5, jQuery
- **Authentification** : ASP.NET Core Identity

## Structure de l'application

- **/Pages** : Pages Razor pour l'interface utilisateur
- **/Data** : Modèles de données et contexte de base de données
- **/Controllers** : API pour les opérations AJAX
- **/Services** : Services métier
- **/wwwroot** : Ressources statiques (CSS, JS, images)
- **/Middleware** : Composants middleware personnalisés

## Configuration et démarrage

### Prérequis

- .NET 6.0 SDK ou supérieur
- Un éditeur de code comme Visual Studio ou VS Code

### Installation

1. Cloner le dépôt

```
git clone [URL_DU_DEPOT]
```

2. Naviguer vers le répertoire du projet

```
cd EMGAS
```

3. Restaurer les packages NuGet

```
dotnet restore
```

4. Exécuter l'application

```
dotnet run
```

5. Accéder à l'application dans un navigateur à l'adresse https://localhost:7000 (ou le port indiqué dans la console)

## Utilisateurs prédéfinis

L'application est préchargée avec les rôles suivants :

- Administrateur : Accès complet à toutes les fonctionnalités
- Manager : Gestion des véhicules et des ventes
- Utilisateur : Consultation uniquement

## Contribution

Pour contribuer au projet, veuillez suivre ces étapes :

1. Forker le projet
2. Créer une branche pour votre fonctionnalité (`git checkout -b feature/ma-nouvelle-fonctionnalite`)
3. Commiter vos changements (`git commit -m 'Ajout de ma nouvelle fonctionnalité'`)
4. Pousser vers la branche (`git push origin feature/ma-nouvelle-fonctionnalite`)
5. Ouvrir une Pull Request

## Licence

OPEN SOURCE
