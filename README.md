# QR WiFi Generator

## Bienvenido a QR WiFi Generator

Genera y almacena códigos QR para tus redes WiFi de forma rápida y sencilla.

---

## ¿Por qué?

Este mini-proyecto nace inicialmente de la necesidad de aprender cómo enviar información entre una aplicación Web MVC en Razor y una aplicación de tipo WebAPI MVC. Este hecho define esta solución como una aplicación Front-End / Back-End en términos de la infraestructura. Fue creada de manera sencilla, sin grandes pretensiones en el aspecto gráfico, sino más bien en el aspecto del funcionamiento, viendo un Back-end un poco más complejo que la típica aplicación con un CRUD.

Esta aplicación implementa no solo la generación de códigos QR sino que además tiene configurado un Background-Worker que funciona en el Back-end para el despacho de los emails usando una cola "thread-safe" y deja libre a la aplicación Front-End una vez que se recibe el código solicitado. Esto permite un alto nivel de requerimientos por segundo sin perjudicar en gran medida las velocidades de respuesta.

Puedes hacer un fork de esta aplicación sin ningún tipo de restricciones, lo único que te pido es que me des el crédito que corresponde mencionándome en tu proyecto. No es que esta solución sea tremendamente innovadora, pero sí tiene varias horas de trabajo que espero al menos tengan un poco de reconocimiento en la comunidad de desarrolladores.

---

## ¿Qué puedes hacer aquí?

- **Generar códigos QR para Redes WiFi:** Introduce los detalles de tu red WiFi y genera un código QR que podrás imprimir o compartir fácilmente.
- **Almacenamiento Seguro:** Todos los códigos QR generados se almacenan en tu cuenta para que puedas volver a acceder a ellos cuando lo necesites.
- **Envío por Correo Electrónico:** Recibe tus códigos QR directamente en tu bandeja de entrada, listos para imprimir o compartir.

---

## ¿Cómo funciona?

1. **Introduce los Detalles de tu red WiFi**
    - Nombre de la Red (SSID)
    - Contraseña
    - Tipo de Seguridad (WPA, WPA2, WEP)

2. **Genera tu código QR**
    - Haz clic en el botón "Generar" para crear tu código QR.

3. **Almacena y Comparte**
    - Guarda automáticamente tus códigos QR en un archivo donde la información de las claves se encuentra encriptada.
    - Envía el código QR al correo electrónico de quien tú quieras.

---

## Configuración

Abre el archivo **"appsettings.json"** para la aplicación "QRCode" y agrega la información donde veas estos valores **[YOUR_NAME]**, etc. Para el **EncryptionKey DEBES usar un arreglo de byte[] como llave**. En alguna versión futura podría reemplazar este dato por una string simple que permita funcionar como llave. Eres libre de cambiar este comportamiento por ti mismo si así lo deseas.
