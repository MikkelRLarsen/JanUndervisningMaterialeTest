# CI/CD Plan for Multi-Repo Microservice System med Aspire

## 1. Introduktion

Dette dokument beskriver den planlagte CI/CD-struktur for et multi-repo .NET microservice system, hvor Aspire anvendes som central integrationstest- og staging-orchestrator.  

Formålet er at sikre:

- Konsistens mellem Dev, Staging og Prod
- Sikker håndtering af artefakter
- Multi-repo coordination
- Kontrolleret release-strategi
- Immutable artefakter og traceability
- Mulighed for feature flags

---

## 2. Repository Struktur

- **Hver microservice har sit eget repo**  
  - Indeholder unit tests, linter, build scripts  
  - Egen CI workflow for push/PR triggers  
- **Aspire repo**  
  - Indeholder system composition / integration orchestration  
  - Orkestrerer integration / E2E tests  
  - Workflow kan trigges fra andre microservice workflows  

---

## 3. CI/CD Pipeline Overview

### 3.1 Microservice CI Workflow

**Trigger:** Push/PR på service repo  
**Steps:**

1. Checkout kode  
2. Restore / Build  
3. Unit tests + linter + security scanning  
4. Build staging artifact (fx container image eller private package)  
5. Tag artefakt med commit SHA / branch + version  
6. Push artefakt til staging host (GHCR, private package repository, eller blob storage)  
7. Trigger Aspire workflow for integrationstest med artefakt-tag som input  

**Output:** Immutable staging artefakt og status til integrationstest  

---

### 3.2 Aspire Integration Workflow

**Trigger:** Repository dispatch / workflow_call fra microservice workflow  
**Steps:**

1. Login til private container registry (PAT / secrets)  
2. Pull staging artifact for relevant microservice  
3. Pull afhængigheder (andre microservices container images)  
4. Start services via AppHost / container orchestration  
5. Kør integration / E2E tests  
6. Return success/failure til calling microservice workflow  

**Output:** Integration-test status, klarhed om artefaktens readiness til prod  

---

### 3.3 Promotion til Prod

**Flow:**

1. Microservice workflow modtager success fra Aspire workflow  
2. Bygger prod-artifact baseret på samme staging artefakt (immutable)  
3. Gem artefakt i container registry / package repository  
4. Deploy til prod sker enten:  
   - Continuous Delivery: automatisk, når integration tests passer  
   - Scheduled Release: ved sprint-end / planlagt release-vindue  

**Feature Flags:**  
- Tillader ikke-godkendte features at eksistere i kodebase  
- Aktiveres først i prod, når artefakt er godkendt  

---

## 4. Artefakt Filosofi

- **Immutable:** Et artefakt bygges én gang og bruges til både staging og prod.  
- **Versioning:** Brug commit SHA, branch, eller semver tags  
- **Host:** Private container registry (GHCR, ACR, ECR) eller package repository  
- **Traceability:** Alle artefakter kan spores tilbage til commits og integration-test status  

---

## 5. Multi-Repo Integration Strategy

- Aspire workflow per microservice for integrationstest  
- Hver workflow kun starter relevante afhængigheder  
- Microservice CI workflow sender staging artefakt info til Aspire workflow  
- Artefakter kan testes isoleret uden at starte hele systemet  
- Workflow status styrer om prod-artifact bygges  

---

## 6. Release Strategy

**Valgfrie modeller:**

### A) Continuous Delivery

- Prod-artifact deployes automatisk efter integrationstest passer  
- Feature flags styrer synlighed i prod  
- Ideel for cloud-native SaaS eller mindre teams  

### B) Scheduled Release / Sprint Release

- Artefakt bygges og testes kontinuerligt  
- Prod-release sker kun på fast plan (fx slutningen af sprint)  
- Giver kontrol og overholdelse af release governance  

**Princip:** Artefakt bygges én gang → integration tests → staging → promotion → prod-release  

---

## 7. Security & Access Control

- Private container registry kræver **PAT / secrets**  
- Aspire workflow logger ind til registry for at hente artefakter  
- Feature flags og secrets sikrer, at kun godkendte features aktiveres i prod  

---

## 8. Best Practices og Principper

- **Single build → multiple environments:** Ingen rebuild mellem staging → prod  
- **Feature flags:** Hold ufuldstændige funktioner skjult i prod  
- **Immutable artefacts:** Alle artefakter er versionsbestemte og kan spores  
- **Integration-test gating:** Prod-artifact bygges kun hvis integration-tests passer  
- **Multi-repo coordination via artefacts / container images:** Undgå at checke kode fra andre repos direkte  

---

## 9. Visualisering af Flow

**Simplificeret flow:**  

