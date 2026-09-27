import { KitDefinition } from './kit.model';

/**
 * Kits armados a mano con productos compatibles entre sí.
 * Si cambian los IDs de producto en el panel, actualiza esta lista.
 * Siguiente paso natural: administrarlos desde el backend.
 */
export const KIT_DEFINITIONS: KitDefinition[] = [
  {
    id: 'acelerar', code: 'SSD', goal: 'Acelerar un PC lento', name: 'Kit revive tu PC',
    note: 'SSD SATA, que sirve en cualquier torre, y 16 GB de RAM DDR4 de escritorio.',
    productIds: [2, 3],
    question: 'Tengo un PC de escritorio lento. ¿Me sirve el kit con SSD SATA 480 GB y RAM DDR4 16 GB? Mi equipo es: '
  },
  {
    id: 'portatil', code: 'LAP', goal: 'Mejorar un portátil', name: 'Kit portátil al día',
    note: 'SSD de 2,5" y 8 GB SODIMM. Revisa que tu portátil use memoria DDR4.',
    productIds: [2, 4],
    question: '¿El kit con SSD SATA 480 GB y RAM SODIMM DDR4 8 GB le sirve a mi portátil? Es un: '
  },
  {
    id: 'gamer', code: 'CPU', goal: 'Armar una base gamer', name: 'Kit base AM4',
    note: 'Ryzen 5 5600, board B550M, 16 GB DDR4 y pasta térmica. Compatibles entre sí.',
    productIds: [5, 8, 3, 9],
    question: 'Quiero armar un PC gamer con el kit Ryzen 5 5600 + B550M + 16 GB. ¿Qué más me falta? Ya tengo: '
  },
  {
    id: 'grafica', code: 'GPU', goal: 'Jugar en 1080p', name: 'Kit gráfica 1080p',
    note: 'RTX 4060 y fuente de 650 W, que cubre los 550 W que pide la tarjeta.',
    productIds: [6, 7],
    question: '¿Puedo instalar la RTX 4060 con la fuente de 650 W en mi equipo? Tengo: '
  }
];
