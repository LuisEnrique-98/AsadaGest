// Comportamiento de las paginas que antes estaba escrito dentro del HTML (onclick="..."). Desde el
// Sprint 6c la politica de contenido (CSP) no deja ejecutar JavaScript dentro del HTML: todo vive aqui.
//
//  - data-confirmar="Pregunta": antes de enviar el formulario se pregunta; con "Cancelar" no se envia.
//  - data-cerrar-menu: al tocar una opcion del menu en una pantalla angosta, el menu se cierra.
//
// Se escucha en el documento (y no en cada boton) porque Blazor reemplaza el contenido de la pagina al
// navegar: asi sigue funcionando en las paginas nuevas sin volver a enlazar nada.
(function () {
    "use strict";

    // Fase de captura: se pregunta antes de que el formulario (o Blazor) reciba el clic.
    document.addEventListener("click", function (evento) {
        var boton = evento.target instanceof Element ? evento.target.closest("[data-confirmar]") : null;
        if (boton && !window.confirm(boton.getAttribute("data-confirmar"))) {
            evento.preventDefault();
            evento.stopPropagation();
        }
    }, true);

    document.addEventListener("click", function (evento) {
        var menu = evento.target instanceof Element ? evento.target.closest("[data-cerrar-menu]") : null;
        if (!menu) return;
        var interruptor = document.querySelector(".navbar-toggler");
        if (interruptor) interruptor.click();
    });
})();
