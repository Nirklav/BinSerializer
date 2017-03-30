### Serialization format:

Simple value type:

| type | description                  |
|--------|----------------------------|
| string | type Name                  | 
| int | type version                  |
| bytes | type data                   |  

---

Simple ref type:

| type | description                  |
|--------|----------------------------|
| string | type Name                  | 
| int | reference id                  | 
| int | type version                  |
| bytes | type data                   |

---

User difined value type:

| type | description                  |
|--------|----------------------------|
| string | type Name                  | 
| int | type version                  |

Fields (if field is null nothing will be written)

| type | description                  |
|--------|----------------------------|
| string | field id                   |
| inner type | starts from type name  |

Other fields

...

Last field

| type | description                  |
|--------|----------------------------|
| string | field id                   |
| inner type | starts from type name  |

End

| type | description                  |
|--------|----------------------------|
| string | TypeEndToken - end of type |

---

User defined ref type:

| type | description                  |
|--------|----------------------------|
| string | type Name                  | 
| int | reference id                  |
| int | type version                  |

Fields (if field is null nothing will be written)

| type | description                  |
|-------|-----------------------------|
| string | field id                   |
| inner type | starts from type name  |

Other fields

...

Last field

| type | description                  |
|--------|----------------------------|
| string | field id                   |
| inner type | starts from type name  |

End

| type | description                  |
|--------|----------------------------|
| string | TypeEndToken - end of type | 

---

Array:

| type | description                  |
|--------|----------------------------|
| string | ArrayToken[elementTypeId]  |
| int | reference id                  |
| int | array length                  |

Array elements

| type | description                  |
|--------|----------------------------|
| inner type | starts from type name  |

Other elements

...

Last element

| type | description                  |
|--------|----------------------------|
| inner type | starts from type name  |

---
   
Reference id field remark:
- 0 for null.
- If reference is null it's will be end of type.
- If reference already exist when you read then it is also will be end of type.