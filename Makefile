.PHONY: up down build test test-all test-integration lint migrate-up init-tls backup restore test-privacy ci zap-scan \
	rpi-buildx-setup rpi-build rpi-save rpi-push rpi-up rpi-down

up:
	docker compose up -d

down:
	docker compose down

build:
	dotnet build FamilyOs.sln

test:
	dotnet test FamilyOs.sln --filter "Category!=Integration"

test-all:
	dotnet test FamilyOs.sln

test-integration:
	dotnet test FamilyOs.sln --filter "Category=Integration"

lint:
	dotnet csharpier --check .

migrate-up:
	dotnet ef database update --project src/FamilyOs.Infrastructure --startup-project src/FamilyOs.Api

fe-install:
	cd frontend && pnpm install

fe-build:
	cd frontend && pnpm build:prod

fe-test:
	cd frontend && pnpm test

fe-lint:
	cd frontend && pnpm lint

fe-e2e:
	cd frontend && pnpm e2e:smoke

fe-e2e-all:
	cd frontend && pnpm e2e

# TLS inicializálás
init-tls:
	@echo "Initializing TLS CA and certificates..."
	@chmod +x scripts/init-tls-ca.sh && ./scripts/init-tls-ca.sh

# Backup futtatás (manuális)
backup:
	@docker compose run --rm backup /etc/periodic/daily/backup

# Restore
restore:
	@echo "Restore: docker compose run --rm backup /restore.sh <file>"
	@docker compose run --rm -v $(PWD)/scripts/restore.sh:/restore.sh:ro backup /restore.sh $(filter-out $@,$(MAKECMDGOALS))

# Privacy assertion teszt (CI red gate)
test-privacy:
	dotnet test tests/FamilyOs.Infrastructure.Ai.Tests/ --filter "Category=PrivacyAssertion" --no-build

# Full CI
ci:
	dotnet build FamilyOs.sln --no-incremental
	dotnet test FamilyOs.sln
	cd frontend && pnpm build --configuration production

# ZAP scan (needs running stack)
zap-scan:
	@docker run --rm -t zaproxy/zap-stable zap-baseline.py \
	  -t http://localhost:8080 \
	  -r zap-report.html || true

# ---- Raspberry Pi (arm64) cross-build — lásd docs/deploy-raspberry-pi.md ----
RPI_TAG ?= arm64-latest
RPI_REGISTRY ?=

# Egyszeri lépés: buildx builder QEMU-emulációval az arm64 cross-buildhez
rpi-buildx-setup:
	docker buildx create --name family-os-rpi --driver docker-container --use 2>/dev/null || docker buildx use family-os-rpi
	docker buildx inspect --bootstrap

# arm64 image-ek buildelése és betöltése a helyi Docker image store-ba
# (csak megtekintésre/exportra jó, natívan nem futtatható amd64 gépen)
rpi-build:
	docker buildx build --platform linux/arm64 -f docker/api.Dockerfile -t family-os-api:$(RPI_TAG) --load .
	docker buildx build --platform linux/arm64 -f docker/workers.Dockerfile -t family-os-workers:$(RPI_TAG) --load .
	docker buildx build --platform linux/arm64 -f docker/web.Dockerfile -t family-os-web:$(RPI_TAG) --load .

# arm64 image-ek exportálása egy tömörített tar-ba, LAN-on belüli átvitelhez
# (scp-vel a Pi-re, majd `docker load -i` — nincs szükség registry-re)
rpi-save: rpi-build
	mkdir -p dist/rpi
	docker save family-os-api:$(RPI_TAG) family-os-workers:$(RPI_TAG) family-os-web:$(RPI_TAG) | gzip > dist/rpi/family-os-images-$(RPI_TAG).tar.gz
	@echo "Kész: dist/rpi/family-os-images-$(RPI_TAG).tar.gz"
	@echo "Másold át a Pi-re, pl.: scp dist/rpi/family-os-images-$(RPI_TAG).tar.gz pi@<pi-ip>:~/"
	@echo "A Pi-n: gunzip -c family-os-images-$(RPI_TAG).tar.gz | docker load"

# Alternatíva rpi-save helyett: közvetlen push egy registrybe (pl. ghcr.io/felhasznalo)
# RPI_REGISTRY beállítása kötelező, előtte `docker login` szükséges.
rpi-push:
	@test -n "$(RPI_REGISTRY)" || (echo "Hiba: add meg a RPI_REGISTRY változót, pl. make rpi-push RPI_REGISTRY=docker.io/felhasznalo/" && exit 1)
	docker buildx build --platform linux/arm64 -f docker/api.Dockerfile -t $(RPI_REGISTRY)family-os-api:$(RPI_TAG) --push .
	docker buildx build --platform linux/arm64 -f docker/workers.Dockerfile -t $(RPI_REGISTRY)family-os-workers:$(RPI_TAG) --push .
	docker buildx build --platform linux/arm64 -f docker/web.Dockerfile -t $(RPI_REGISTRY)family-os-web:$(RPI_TAG) --push .

# A Pi-n futtatandó parancsok (dokumentáció, nem helyi végrehajtásra szánt):
rpi-up:
	docker compose -f docker-compose.yml -f docker-compose.rpi.yml up -d --no-build

rpi-down:
	docker compose -f docker-compose.yml -f docker-compose.rpi.yml down
