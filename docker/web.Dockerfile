FROM node:22-alpine AS build
WORKDIR /app
COPY frontend/package.json frontend/package-lock.json ./
# --legacy-peer-deps: a package-lock.json-ban van egy @angular/animations
# <-> @angular/common peer-verzió ütközés, amit a helyi fejlesztői
# node_modules toleránsabb (régebbi) npm-mel installálva sosem ütött ki,
# de tiszta konténerbeli telepítésnél (node:22-alpine friss npm-mel) elakad.
# Ez a build-időre szűkített workaround, nem oldja meg a lockfile-t.
RUN npm install --legacy-peer-deps
COPY frontend/ .
RUN npm run build:prod

FROM nginx:1.25-alpine AS runtime
COPY --from=build /app/dist/family-os/browser /usr/share/nginx/html
COPY docker/nginx/family-os.conf /etc/nginx/conf.d/default.conf
EXPOSE 80 443
