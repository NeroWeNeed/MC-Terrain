#ifndef CHUNKDATA_OFFSET
#define CHUNKDATA_OFFSET 8
#endif
#ifndef CHUNK_HORIZONTAL_SPAN
#define CHUNK_HORIZONTAL_SPAN 8
#endif
#ifndef CHUNK_VERTICAL_SPAN
#define CHUNK_VERTICAL_SPAN 16
#endif
#ifndef CHUNK_LAYER
#define CHUNK_LAYER (CHUNK_HORIZONTAL_SPAN*CHUNK_HORIZONTAL_SPAN)
#endif
#ifndef CHUNK_BOX
#define CHUNK_BOX (CHUNK_HORIZONTAL_SPAN*CHUNK_HORIZONTAL_SPAN*CHUNK_VERTICAL_SPAN)
#endif
#ifndef CHUNK_DATA_SIZE
#define CHUNK_DATA_SIZE (CHUNK_BOX+CHUNKDATA_OFFSET)
#endif
#ifndef PADDED_CHUNK_HORIZONTAL_SPAN
#define PADDED_CHUNK_HORIZONTAL_SPAN (CHUNK_HORIZONTAL_SPAN+2)
#endif
#ifndef PADDED_CHUNK_VERTICAL_SPAN
#define PADDED_CHUNK_VERTICAL_SPAN (CHUNK_VERTICAL_SPAN+2)
#endif
#ifndef PADDED_CHUNK_LAYER
#define PADDED_CHUNK_LAYER (PADDED_CHUNK_HORIZONTAL_SPAN*PADDED_CHUNK_HORIZONTAL_SPAN)
#endif
#ifndef PADDED_CHUNK_BOX
#define PADDED_CHUNK_BOX (PADDED_CHUNK_HORIZONTAL_SPAN*PADDED_CHUNK_HORIZONTAL_SPAN*PADDED_CHUNK_VERTICAL_SPAN)
#endif
#ifndef PADDED_CHUNK_DATA_SIZE
#define PADDED_CHUNK_DATA_SIZE (PADDED_CHUNK_BOX+CHUNKDATA_OFFSET)
#endif
#ifndef VERTEX_ORDER
#define VERTEX_ORDER
static const uint3 vertexOrder[8] = {
    {0,0,0},
    {1,0,0},
    {0,0,1},
    {1,0,1},
    {0,1,0},
    {1,1,0},
    {0,1,1},
    {1,1,1}
};
#endif