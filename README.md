# Gestion Absences API

Cette application est une API de gestion des absences et des réservations. Elle est développée avec **ASP.NET Core** (.NET 9.0) et utilise **Entity Framework Core** pour interagir avec une base de données **MySQL/MariaDB**. Voici les principales fonctionnalités :
- Gestion des utilisateurs (admin, étudiants, professeurs, etc.).
- Authentification avec **JWT** pour sécuriser les endpoints.
- Initialisation automatique d'un utilisateur administrateur si la base est vide.
- Documentation automatique de l'API avec **Swagger**.

---

## Prérequis

Assurez-vous que les éléments suivants sont installés sur votre machine pour exécuter ce projet :

- **SDK .NET** : Version **9.0** (Recommandé : v9.0.100+).
- **Base de données** : MariaDB 10.11 ou MySQL 8.x.
- **IDE** : [JetBrains Rider](https://www.jetbrains.com/rider/) ou Visual Studio 2022.
- **Entity Framework CLI** : Inclus dans le projet (via `dotnet ef`).

---

## Configuration

Le fichier `appsettings.json` contient la configuration de la base de données et des paramètres de sécurité pour JWT.

### Contenu de `appsettings.json`

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": "Server=82.66.203.90;Database=gestion_absences;User=user_abs;Password=%@8Sm1chel/#$%^3412;"
  },
  "JwtSettings": {
    "Key": "eyJ0eXAiOiJKV1QiLCJhbGciOiJIUzI1NiJ9.eyJqdGkiOiI5NjQ5MmQ1OS0wYWQ1LTRjMDAtODkyZC01OTBhZDVhYzAwZjMiLCJzdWIiOiIwMTIzNDU2Nzg5IiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNjgxMDQwNTE1fQ.p8DXyu99_K1XjwQZHiD4Y7EvrZOp12zsZdPdv5tAo8I",
    "Issuer": "gest_abs",
    "Audience": "gest_abs_users",
    "DurationInMinutes": 60
  },
  "AllowedHosts": "*"
}
```

---

## Fonctionnalités principales

- **Authentification avec JWT** :
  - Les utilisateurs se connectent à l'API grâce à un email et un mot de passe.
  - Un token JWT est généré lors de la connexion réussie, incluant des informations sur l'utilisateur (email et rôle).

- **Gestion des rôles utilisateurs** :
  - Rôles disponibles : `admin`, `eleve`, `professeur`, etc.
  - Chaque rôle peut accéder à certains endpoints selon les authorisations configurées.

- **Swagger** :
  - La documentation interactive est disponible grâce à **Swagger**, permettant de tester directement les endpoints.

---

## Installation et démarrage

### 1. Cloner le projet



### 2. Configurer la base de données

Ajoutez/configurez une base de données MariaDB/MySQL. Vérifiez que la chaîne de connexion dans le fichier `appsettings.json` correspond bien aux paramètres de votre base de données.

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=82.66.203.90;Database=gestion_absences;User=user_abs;Password=%@8Sm1chel/#$%^3412;"
}
```

### 3. Restaurer les dépendances NuGet

Pour installer les packages nécessaires, exécutez :

```bash
dotnet restore
```

### 4. Appliquer les migrations à la base de données

Pour créer le schéma de la base de données, exécutez :

```bash
dotnet ef database update
```

### 5. Lancer le projet

Démarrez l'application avec la commande suivante :

```bash
dotnet run
```

Le projet est maintenant accessible à [http://localhost:5000](http://localhost:5000) (ou un autre port configuré).

---

## Endpoints principaux

### 1. **Authentification**
- **POST `/api/auth/login`** : Permet de se connecter et d'obtenir un token JWT.

**Exemple de requête :**

```json
POST http://localhost:5000/api/auth/login
Content-Type: application/json

{
  "username": "admin",
  "password": "admin123"
}
```

**Exemple de réponse réussie :**

```json
{
  "token": "eyJhbGciOiJIUzI1NiIsIn...",
  "username": "admin",
  "role": "admin"
}
```

---

## Organisation du projet

### 1. **Modèles**
Les modèles définissent les entités principales de la base de données. Exemples :
- **User** : Gère les utilisateurs avec des attributs comme `Email`, `Password`, `Role`.
- **Absence** : Gère les absences des étudiants (raison, statut, etc.).
- **Reservation** : Permet de gérer les réservations de ressources (salles, etc.).

### 2. **Contrôleurs**
Chaque contrôleur correspond à un groupe d'opérations pour l'API :
- **AuthController** : Authentification et gestion des tokens JWT.
- **AbsenceController** : Tracking et gestion des absences.
- **ReservationController** : Gestion des réservations d'objets.

---

## Documentation avec Swagger

Swagger est intégré pour permettre d'afficher la liste des endpoints avec leur documentation détaillée. Vous pouvez également tester les endpoints directement depuis l'interface Swagger.

### URL Swagger :
[http://localhost:5196/home](http://localhost:5196/home)

---

## Versions utilisées

- **.NET SDK** : 9.0.100
- **Mariadb/MySQL** : 10.11.x ou 8.0.x
- **EF Core** : 7.x
- **Swagger** : Dernière version compatible avec ASP.NET Core

---

## Améliorations possibles et TODO

- [ ] Ajouter des tests unitaires pour les contrôleurs.
- [ ] Implémenter des middlewares pour une gestion centralisée des erreurs.
- [ ] Limiter les champs retournés par l'API via des DTO pour des réponses plus performantes.
- [ ] Ajouter une politique `Role-Base Access Control` plus robuste pour sécuriser les endpoints.
- [ ] Ajouter des fonctionnalités pour les notifications ou alertes en temps réel (ex : WebSockets).

---

## Auteur

- **Nom** : *Votre nom ici*  
- **Email** : *Votre email ici ("facultatif si repo publique")*  
