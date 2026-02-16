---

# CI/CD Plan for Multi-Repo Microservice System med Aspire + DevSecOps

## 1. Introduktion

Dette dokument beskriver den planlagte CI/CD-struktur for et multi-repo .NET microservice system, hvor Aspire anvendes som central integrationstest- og staging-orchestrator, og hvor **DevSecOps er integreret** i hele workflowet.

Formålet er at sikre:

* Konsistens mellem Dev, Staging og Prod
* Sikker håndtering af artefakter og pipelines
* Multi-repo coordination
* Kontrolleret release-strategi
* Immutable artefakter og traceability
* Mulighed for feature flags
* Automatisk sikkerhedsscanning af kode, dependencies, container images og runtime

---

## 2. Repository Struktur

* **Hver microservice har sit eget repo**

  * Indeholder unit tests, linter, build scripts
  * Egen CI workflow for push/PR triggers
  * Security-scanning inkluderet (SAST, dependency scan)
* **Aspire repo**

  * Indeholder system composition / integration orchestration
  * Orkestrerer integration / E2E tests
  * Workflow kan trigges fra andre microservice workflows
  * Runtime security checks og kontrollerede feature flags

---

## 3. CI/CD Pipeline Overview med DevSecOps

### 3.1 Microservice CI Workflow

**Trigger:** Push/PR på service repo
**Steps:**

1. Checkout kode
2. **Static Application Security Testing (SAST) + kodekvalitetstest**

   * Fx Roslyn analyzers, SonarQube, Semgrep
   * Fail pipeline, hvis kritiske issues findes
3. Restore / Build
4. Unit tests + linter
5. **Dependency scanning / SBOM generation**

   * Fx OWASP Dependency-Check, Dependabot, NuGet audit
   * Stop pipeline ved kritiske CVEs
6. Build staging artifact (fx container image eller private package)
7. **Container security scan**

   * Fx Trivy eller Grype
   * Fail pipeline, hvis container har kritiske sårbarheder
8. Tag artefakt med commit SHA / branch + version
9. Push artefakt til staging host (GHCR, private package repository, blob storage)
10. Trigger Aspire workflow for integrationstest med artefakt-tag som input

**Output:** Immutable, sikker staging artefakt og status til integrationstest

---

### 3.2 Aspire Integration Workflow

**Trigger:** Repository dispatch / workflow_call fra microservice workflow
**Steps:**

1. Login til private container registry (PAT / secrets)
2. Pull staging artifact for relevant microservice
3. Pull afhængigheder (andre microservices container images)
4. **Start services med Dapr sidecars** (kun relevante services, konfigureret via env vars)
5. **Runtime security checks:**

   * Container capabilities, secrets, network isolation
   * Monitorering af Dapr sidecars
6. Kør integration / E2E tests
7. Return success/failure til calling microservice workflow

**Output:** Integration-test status + sikkerhedsstatus, klarhed om artefaktens readiness til prod

---

### 3.3 Promotion til Prod

**Flow:**

1. Microservice workflow modtager success fra Aspire workflow
2. **Kontroller alle sikkerhedsscans er bestået**
3. Bygger prod-artifact baseret på samme staging artefakt (immutable)
4. Gem artefakt i container registry / package repository
5. Deploy til prod sker enten:

   * Continuous Delivery: automatisk, når integration tests og security gates passer
   * Scheduled Release: ved sprint-end / planlagt release-vindue

**Feature Flags:**

* Tillader ikke-godkendte features at eksistere i kodebase
* Aktiveres først i prod, når artefakt er godkendt

---

## 4. Artefakt Filosofi

* **Immutable:** Et artefakt bygges én gang og bruges til både staging og prod
* **Versioning:** Commit SHA, branch, semver tags
* **Host:** Private container registry (GHCR, ACR, ECR) eller package repository
* **Traceability:** Alle artefakter kan spores tilbage til commits, integration-test og security-scan status
* **Security status:** Artefakt kan kun promoveres, hvis alle SAST, dependency og container scans er bestået

---

## 5. Multi-Repo Integration Strategy

* Aspire workflow per microservice for integrationstest
* Hver workflow kun starter relevante afhængigheder
* Microservice CI workflow sender staging artefakt info + security metadata til Aspire workflow
* Artefakter kan testes isoleret uden at starte hele systemet
* Workflow status styrer om prod-artifact bygges

---

## 6. Release Strategy

**Valgfrie modeller:**

### A) Continuous Delivery

* Prod-artifact deployes automatisk efter integrationstest og security gates passer
* Feature flags styrer synlighed i prod

### B) Scheduled Release / Sprint Release

* Artefakt bygges og testes kontinuerligt
* Prod-release sker kun på fast plan (fx slutningen af sprint)

**Princip:** Artefakt bygges én gang → sikkerhedsscanning + integration tests → staging → promotion → prod-release

---

## 7. Security & Access Control

* Private container registry kræver **PAT / secrets**
* Aspire workflow logger ind til registry for at hente artefakter
* Feature flags og secrets sikrer, at kun godkendte features aktiveres i prod
* DevSecOps integration inkluderer:

  * SAST (kode + kvalitet)
  * Dependency scanning (NuGet / package CVEs)
  * Container scanning (Trivy / Grype)
  * Runtime security checks under Aspire orchestration

---

## 8. Best Practices og Principper

* **Single build → multiple environments:** Ingen rebuild mellem staging → prod
* **Feature flags:** Hold ufuldstændige funktioner skjult i prod
* **Immutable artefacts:** Alle artefakter er versionsbestemte, sikkerhedsgodkendte og kan spores
* **Integration-test gating + security gating:** Prod-artifact bygges kun hvis alle integration-tests og security checks passer
* **Multi-repo coordination via artefacts / container images:** Undgå at checke kode fra andre repos direkte

---

## 9. Visualisering af Flow

**Simplificeret flow med DevSecOps:**

```
Microservice Repo
   └─ CI Workflow:
        - Checkout kode
        - SAST / kodekvalitet
        - Dependency scan (NuGet CVEs)
        - Build + unit tests
        - Container scan (Trivy / Grype)
        - Build staging artifact (immutable)
        - Push artifact → GHCR / private registry
        - Trigger Aspire workflow (integration + runtime security)

Aspire Repo Workflow
   └─ Pull staging image + dependencies
   └─ Start AppHost / Dapr sidecars (kun relevante services)
   └─ Runtime security checks
   └─ Run integration / E2E tests
   └─ Return status → Microservice workflow

Microservice Workflow
   └─ If integration + security checks passed → Build prod-artifact (immutable)
   └─ Deploy → Prod (Continuous Delivery / Scheduled Release)
```

---
