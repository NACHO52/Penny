# Penny

Penny será una web multiusuario para registrar ingresos y gastos, consultar cuánto dinero hay en la cuenta y ver reportes mensuales por categoría. Cada usuario tendrá su propio acceso y manejará sus finanzas de forma independiente.

## Primera versión

- Una cuenta financiera con saldo inicial por usuario. Cada movimiento se vincula a esa cuenta para poder sumar otras en el futuro.
- La cuenta, los movimientos, las categorías y subcategorías, los resúmenes de tarjeta y los reportes son privados de cada usuario. El servidor aplica este aislamiento en todas las operaciones sobre esos datos.
- Carga manual de ingresos y gastos con fecha, importe, detalle, categoría y subcategoría.
- Saldo actual, ingresos, gastos y resultado del mes. El saldo inicial forma parte del saldo de la cuenta y se excluye de los ingresos del mes.
- Historial editable y reportes con desglose por categoría.
- El Excel de 2026 sirve como guía. Penny empieza sin importar datos.

## Tarjeta de crédito

- Pendiente para una etapa posterior. La opción Tarjeta queda oculta en la navegación hasta que retomemos esta funcionalidad.
- Al recibir el resumen, se cargan sus consumos uno por uno con importe, detalle y categoría. La fecha de compra puede conservarse en el detalle.
- Los consumos se incluyen en los reportes del mes del resumen. Una compra de enero que figura en el resumen de febrero se reporta en febrero.
- El total queda pendiente hasta registrar el pago. El pago reduce el saldo de la cuenta en su fecha y los consumos se contabilizan una sola vez en los reportes.
- El inicio muestra el total pendiente de tarjeta cuando corresponde.
- El seguimiento de cuotas queda para más adelante. Si una cuota aparece en el resumen, se carga el importe de esa cuota.

## Tecnología

- Frontend con Vue 3, TypeScript y Vite.

## Convenciones de código

- El código del frontend y del backend usa inglés para todos los identificadores, incluidos los nombres de variables, constantes, funciones, métodos, clases, objetos, propiedades, entidades, archivos de código y rutas de la API. Los comentarios del código también se escriben en inglés.
- La autoría del código se atribuye a las personas responsables. No se acredita a una IA ni a un agente como autor o coautor en el código, la documentación, los commits, los pull requests ni otros artefactos del proyecto.

## Primera fase: acceso de usuarios

- Dar de alta usuarios y verificar que se guarden en SQL Server.
- Probar el inicio y el cierre de sesión desde Vue con ASP.NET Core Identity.
- Verificar que una pantalla privada requiera iniciar sesión y que cada usuario vea su propia sesión.
- La cuenta financiera, el saldo inicial y los movimientos se incorporan en fases posteriores.

## Seguridad

- Las credenciales de la base, los tokens y las claves privadas se guardan fuera de Git. En desarrollo, Penny usa .NET User Secrets para `ConnectionStrings:PennyDatabase`. El README muestra una conexión a SQL Server local con autenticación de Windows y sin contraseña.
- Git ignora `.env` y `.env.*` en todo el proyecto, con una excepción para `.env.example`. Si una credencial se sube por error, hay que revocarla o rotarla, porque borrar el archivo deja el valor en el historial.
- El 27 de noviembre de 2026 se revisaron los archivos versionados y los 21 commits disponibles localmente. No se encontraron credenciales en esa revisión.
- La revisión estática de la API verificó que las cuentas y categorías se filtran por el usuario autenticado, que los movimientos se validan por la cuenta y que las subcategorías se validan por su categoría. El procedimiento SQL del reporte mensual también verifica quién es dueño de la cuenta.
- Queda pendiente una prueba de integración local con dos usuarios para confirmar que cada uno puede operar sobre sus propios datos y que la API rechaza el acceso a registros del otro.
