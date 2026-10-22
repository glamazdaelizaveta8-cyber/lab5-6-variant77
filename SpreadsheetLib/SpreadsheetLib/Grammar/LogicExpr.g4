/*
 * Варіант 77: операції/функції наборів 5, 8, 10, 11, 12
 *   5)  inc(x), dec(x)
 *   8)  =, <, >
 *   10) not
 *   11) or, and
 *   12) eqv
 * Вираз логічний. Операнди: цілі числа довільної довжини, імена клітинок, дужки.
 *
 * Пріоритет (від найвищого до найнижчого):
 *   дужки, inc/dec  >  = < >  >  not  >  and  >  or  >  eqv
 */
grammar LogicExpr;

compileUnit : expr EOF ;

expr    : orExpr (EQV orExpr)* ;
orExpr  : andExpr (OR andExpr)* ;
andExpr : notExpr (AND notExpr)* ;

notExpr
    : NOT notExpr        # NotOp
    | cmpExpr            # ToCmp
    ;

cmpExpr : term (op=(EQ | LT | GT) term)? ;

term
    : INT                                   # IntLit
    | CELL                                  # CellRef
    | LPAREN expr RPAREN                    # Parens
    | func=(INC | DEC) LPAREN expr RPAREN   # FuncCall
    ;

// ---------- Лексер (ключові слова нечутливі до регістру) ----------
EQV : E Q V ;
OR  : O R ;
AND : A N D ;
NOT : N O T ;
INC : I N C ;
DEC : D E C ;

EQ : '=' ;
LT : '<' ;
GT : '>' ;
LPAREN : '(' ;
RPAREN : ')' ;

INT  : [0-9]+ ;
CELL : [a-zA-Z]+ [0-9]+ ;     // наприклад A3, bc12

WS : [ \t\r\n]+ -> skip ;

fragment A : [aA] ;
fragment C : [cC] ;
fragment D : [dD] ;
fragment E : [eE] ;
fragment I : [iI] ;
fragment N : [nN] ;
fragment O : [oO] ;
fragment Q : [qQ] ;
fragment R : [rR] ;
fragment T : [tT] ;
fragment V : [vV] ;
