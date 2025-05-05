using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using ChessAIProject;
using System;
using TMPro;
using System.ComponentModel;

public class AIBoardScript : MonoBehaviour
{
    private Board board = new Board();
    [Header("Board Settings")]
    public float squareSize = 1.0f;
    public GameObject squarePrefab;
    public Color lightColor;
    public Color darkColor;
    public Color selectedColor = Color.yellow;
    public Color possibleMoveColor = Color.green;

    private Dictionary<Vector2Int, GameObject> pieceObjects = new Dictionary<Vector2Int, GameObject>();

    [Header("Piece Sprites")]
    public Sprite[] whitePieces; // [King, Queen, Rook, Bishop, Knight, Pawn]
    public Sprite[] blackPieces; // [King, Queen, Rook, Bishop, Knight, Pawn]

    public TextMeshProUGUI stateText;

    private GameObject[,] squares = new GameObject[8, 8];
    private GameObject[,] pieces = new GameObject[8, 8];
    private Vector2Int? selectedSquare = null; // Pozice vybran�ho pole (null = nic nen� vybr�no)
    private bool isPromoting = false;

    void Start()
    {
        board.SetUpBoard("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1 ");
        CreateGraphicalBoard();
        SetupPieces(); // Na�ten� figur
    }

    void CreateGraphicalBoard()
    {
        for (int file = 0; file < 8; file++)
        {
            for (int rank = 0; rank < 8; rank++)
            {
                bool isLightSquare = (file + rank) % 2 != 0;
                Color color = isLightSquare ? lightColor : darkColor;
                Vector2 position = new Vector2(file * squareSize, rank * squareSize);

                GameObject square = DrawSquare(position, color, file, rank);
                squares[file, rank] = square;

                // P�id�n� collideru pro klik�n�
                BoxCollider2D collider = square.AddComponent<BoxCollider2D>();
                collider.size = new Vector2(squareSize, squareSize);
            }
        }
    }

    GameObject DrawSquare(Vector2 position, Color color, int file, int rank)
    {
        GameObject square = Instantiate(squarePrefab, position, Quaternion.identity, transform);
        square.name = $"Square_{file}_{rank}";
        square.GetComponent<SpriteRenderer>().color = color;
        return square;
    }

    void SetupPieces()
    {
        // Projdi v�ech 64 pol� na �achovnici
        for (int rank = 0; rank < 8; rank++)
        {
            for (int file = 0; file < 8; file++)
            {
                // P�eveden� 2D pozice na line�rn� index (0-63)
                int squareIndex = rank * 8 + file;

                // Z�sk�n� symbolu figurky z logick� �achovnice
                char pieceSymbol = board.GetPieceAt(squareIndex);

                // Pokud je pole pr�zdn�, pokra�uj na dal��
                if (pieceSymbol == '\0') continue;

                // Ur�en� typu a barvy figurky
                PieceType pieceType = GetPieceType(pieceSymbol);
                bool isWhite = char.IsUpper(pieceSymbol);

                // Vytvo�en� grafick� reprezentace figurky
                AddPiece(file, rank, pieceType, isWhite);
            }
        }
    }

    PieceType GetPieceType(char pieceSymbol)
    {
        // P�eveden� symbolu figurky na enum PieceType
        switch (char.ToLower(pieceSymbol))
        {
            case 'k': return PieceType.King;
            case 'q': return PieceType.Queen;
            case 'r': return PieceType.Rook;
            case 'b': return PieceType.Bishop;
            case 'n': return PieceType.Knight;
            case 'p': return PieceType.Pawn;
            default: return PieceType.Pawn; // Fallback, nem�lo by nastat
        }
    }

    void AddPiece(int file, int rank, PieceType type, bool isWhite)
    {
        Vector2 position = new Vector2(file * squareSize, rank * squareSize);
        GameObject piece = new GameObject($"{type}_{file}_{rank}");
        piece.transform.position = position;
        piece.transform.position = new Vector3(position.x, position.y, -1);
        piece.transform.SetParent(transform);
        piece.tag = "Piece";
        SpriteRenderer renderer = piece.AddComponent<SpriteRenderer>();
        renderer.sprite = isWhite ? whitePieces[(int)type] : blackPieces[(int)type];
        renderer.sortingOrder = 1; // Aby byly figurky nad pol��ky

        pieces[file, rank] = piece;

        // P�id�n� collideru pro klik�n�
        BoxCollider2D collider = piece.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(squareSize, squareSize);
        pieceObjects[new Vector2Int(file, rank)] = piece; // Ulo�en� reference
    }
    public void RemovePiece(Vector2Int position)
    {
        if (pieceObjects == null)
        {
            Debug.LogError("PieceObjects dictionary is null!");
            return;
        }

        if (pieceObjects.TryGetValue(position, out GameObject piece))
        {
            Destroy(piece);
            pieceObjects.Remove(position);
        }
        else
        {
            Debug.LogWarning($"Tried to remove non-existent piece at {position}");
        }
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0)) // Kliknut� lev�m tla��tkem
        {
            HandleClick();
        }
    }

    void HandleClick()
    {
        if (isPromoting) return;
        Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        RaycastHit2D hit = Physics2D.Raycast(mousePos, Vector2.zero);
        if (hit.collider != null)
        {
            GameObject clickedObject = hit.collider.gameObject;
            int file = (int)(clickedObject.transform.position.x / squareSize);
            int rank = (int)(clickedObject.transform.position.y / squareSize);
            Vector2Int squarePos = new Vector2Int(file, rank);

            Debug.Log(clickedObject.name);
            // Kliknut� na figurku
            if (clickedObject.CompareTag("Piece"))
            {
                SelectSquare(squarePos);
            }
            // Kliknut� na pole
            else if (clickedObject.CompareTag("Square") && selectedSquare.HasValue)
            {
                OnSquareClicked(squarePos);
            }
        }
    }

    void OnSquareClicked(Vector2Int squarePos)
    {
        if (!selectedSquare.HasValue) return;

        List<Move> moves = MoveGenerator.GenerateLegalMoves(this.board);
        int fromIndex = selectedSquare.Value.x + selectedSquare.Value.y * 8;
        int toIndex = squarePos.x + squarePos.y * 8;

        foreach (Move move in moves)
        {
            if (move.FromSquare == fromIndex && move.ToSquare == toIndex)
            {
                if (move.Flag == MoveFlag.PromotionToQueen ||
                    move.Flag == MoveFlag.PromotionToRook ||
                    move.Flag == MoveFlag.PromotionToBishop ||
                    move.Flag == MoveFlag.PromotionToKnight)
                {
                    bool isCapture = pieceObjects.ContainsKey(squarePos);

                    if (isCapture)
                    {
                        StartCoroutine(SmoothMoveWithCapture(selectedSquare.Value, squarePos, () =>
                        {
                            ShowPromotionUI(selectedSquare.Value, squarePos, move.Flag);
                        }));
                    }
                    else
                    {
                        Vector2Int pos = selectedSquare.Value;
                        StartCoroutine(SmoothMovePiece(selectedSquare.Value, squarePos, () =>
                        {
                            ShowPromotionUI(pos, squarePos, move.Flag);
                        }));
                    }
                }
                else if (move.Flag != MoveFlag.CastlingKingSide && move.Flag != MoveFlag.CastlingQueenSide)
                {
                    StartCoroutine(SmoothMovePiece(selectedSquare.Value, squarePos, () =>
                    {
                        CompleteMove(move);
                    }));
                }
                else
                {
                    CompleteMove(move);
                }
                break;
            }
        }
    }

    IEnumerator SmoothMoveWithCapture(Vector2Int from, Vector2Int to, Action onComplete)
    {
        ResetAllHighlights();
        float duration = 0.2f; // Krat�� doba pro bran�
        float elapsed = 0f;

        Vector2 startPos = new Vector2(from.x * squareSize, from.y * squareSize);
        Vector2 endPos = new Vector2(to.x * squareSize, to.y * squareSize);

        GameObject movingPiece = pieceObjects[from];
        GameObject capturedPiece = pieceObjects[to];

        // Odstran�n� ze slovn�ku p�ed animac�
        pieceObjects.Remove(from);
        pieceObjects.Remove(to);

        // Animace pohybu
        while (elapsed < duration)
        {
            movingPiece.transform.position = Vector2.Lerp(startPos, endPos, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Dokon�en� pohybu
        movingPiece.transform.position = endPos;
        pieceObjects[to] = movingPiece;

        // Zni�en� sebran� figurky
        Destroy(capturedPiece);

        onComplete?.Invoke();
    }

    IEnumerator SmoothMovePiece(Vector2Int from, Vector2Int to, Action onComplete)
    {
        ResetAllHighlights();

        if (!pieceObjects.ContainsKey(from))
        {
            Debug.LogError($"No piece at {from} to move!");
            yield break;
        }

        float duration = 0.3f;
        float elapsed = 0f;
        GameObject piece = pieceObjects[from];

        // Okam�it� odstran�n� ze slovn�ku
        pieceObjects.Remove(from);

        Vector2 startPos = new Vector2(from.x * squareSize, from.y * squareSize);
        Vector2 endPos = new Vector2(to.x * squareSize, to.y * squareSize);

        while (elapsed < duration)
        {
            piece.transform.position = Vector2.Lerp(startPos, endPos, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        piece.transform.position = endPos;

        // Bezpe�n� p�id�n� do nov� pozice
        if (pieceObjects.ContainsKey(to))
        {
            Destroy(pieceObjects[to]);
        }
        pieceObjects[to] = piece;

        onComplete?.Invoke();
    }

    void CompleteMove(Move move)
    {
        board.MakeMove(move);

        // Speci�ln� logika pro prom�nu
        if (move.Flag == MoveFlag.PromotionToQueen ||
            move.Flag == MoveFlag.PromotionToRook ||
            move.Flag == MoveFlag.PromotionToBishop ||
            move.Flag == MoveFlag.PromotionToKnight)
        {
            int file = move.ToSquare % 8;
            int rank = move.ToSquare / 8;
            Vector2Int pos = new Vector2Int(file, rank);

            // Odstran�n� p�ce
            RemovePiece(pos);

            // Vytvo�en� nov� figurky podle typu prom�ny
            PieceType promotedType = move.Flag switch
            {
                MoveFlag.PromotionToQueen => PieceType.Queen,
                MoveFlag.PromotionToRook => PieceType.Rook,
                MoveFlag.PromotionToBishop => PieceType.Bishop,
                MoveFlag.PromotionToKnight => PieceType.Knight,
                _ => PieceType.Queen
            };

            AddPiece(file, rank, promotedType, board.IsWhiteTurn);
        }
        else
        {
            ResetBoard();
            SetupPieces();
        }
        if (board.IsDrawingMaterial())
        {
            stateText.text = "Draw";
            return;
        }
        List<Move> moves = MoveGenerator.GenerateLegalMoves(board);
        Debug.Log(moves.Count + " moves");
        if (moves.Count == 0)
        {
            ulong kingMask = board.IsWhiteTurn ? board.WhiteKings : board.BlackKings;
            int kingSquare = BitBoardUtils.TrailingZeroCount(kingMask);

            ulong attackers = MoveGenerator.GetAttackers(board, kingSquare, !board.IsWhiteTurn);
            if (attackers > 0)
            {
                stateText.text = board.IsWhiteTurn ? "Black won!" : "White won!";

            }
            else
            {
                stateText.text = "Draw";
            }
        }
    }

    void SelectSquare(Vector2Int squarePos)
    {
        // Zkontroluj, zda je figurka na tahu
        int squareIndex = squarePos.x + squarePos.y * 8;
        char piece = board.GetPieceAt(squareIndex);
        bool isPieceWhite = char.IsUpper(piece);

        if ((isPieceWhite && !board.IsWhiteTurn) || (!isPieceWhite && board.IsWhiteTurn))
        {
            // Pokus o proveden� tahu na figurku protihr��e
            if (selectedSquare.HasValue)
            {
                int fromIndex = selectedSquare.Value.x + selectedSquare.Value.y * 8;
                List<Move> moves = MoveGenerator.GenerateLegalMoves(this.board);
                foreach (Move move in moves)
                {
                    if (move.FromSquare == fromIndex && move.ToSquare == squareIndex)
                    {
                        Debug.Log(move);
                        if (move.Flag == MoveFlag.PromotionToQueen ||
                            move.Flag == MoveFlag.PromotionToRook ||
                            move.Flag == MoveFlag.PromotionToBishop ||
                            move.Flag == MoveFlag.PromotionToKnight)
                        {
                            // Zobraz UI pro v�b�r prom�ny
                            ShowPromotionUI(selectedSquare.Value, squarePos, move.Flag);
                        }
                        else if (move.Flag != MoveFlag.CastlingKingSide && move.Flag != MoveFlag.CastlingQueenSide)
                        {
                            StartCoroutine(SmoothMoveWithCapture(selectedSquare.Value, squarePos, () =>
                            {
                                CompleteMove(move);
                            }));
                        }
                        else
                        {
                            CompleteMove(move);
                        }
                        break;
                    }
                }
            }
            ResetAllHighlights();
            return;
        }


        if (!(selectedSquare.HasValue && squarePos == selectedSquare.Value))
        {
            ResetAllHighlights();
            selectedSquare = squarePos;
            // Zv�razni nov� vybran� pole
            squares[squarePos.x, squarePos.y].GetComponent<SpriteRenderer>().color = selectedColor;
            ShowPossibleMoves(squarePos);
        }
        else
        {
            ResetAllHighlights();
        }
    }


    void ResetAllHighlights()
    {
        // Reset v�ech pol� na p�vodn� barvu
        for (int file = 0; file < 8; file++)
        {
            for (int rank = 0; rank < 8; rank++)
            {
                ResetSquareColor(new Vector2Int(file, rank));
            }
        }

        // Reset v�b�ru
        selectedSquare = null;
    }
    void ResetBoard()
    {
        foreach (var pieceEntry in pieceObjects)
        {
            Destroy(pieceEntry.Value);
        }
        pieceObjects.Clear();

        pieces = new GameObject[8, 8];
        ResetAllHighlights();

    }


    void ShowPossibleMoves(Vector2Int fromSquare)
    {
        List<Move> moves = MoveGenerator.GenerateLegalMoves(this.board);
        int fromIndex = fromSquare.x + fromSquare.y * 8;

        foreach (Move move in moves)
        {
            if (move.FromSquare != fromIndex) continue;

            int toFile = move.ToSquare % 8;
            int toRank = move.ToSquare / 8;

            // Debug log pro kontrolu
            Debug.Log($"Possible move from ({fromSquare.x},{fromSquare.y}) to ({toFile},{toRank})");

            Highlight(toFile, toRank);
        }
    }
    public void Highlight(int file, int rank, Color color)
    {
        squares[file, rank].GetComponent<SpriteRenderer>().color = color;
    }
    public void Highlight(int file, int rank)
    {
        squares[file, rank].GetComponent<SpriteRenderer>().color = possibleMoveColor;
    }
    void ResetSquareColor(Vector2Int pos)
    {
        bool isLight = (pos.x + pos.y) % 2 != 0;
        squares[pos.x, pos.y].GetComponent<SpriteRenderer>().color = isLight ? lightColor : darkColor;
    }


    [Header("Promotion UI")]
    public GameObject promotionPanel;
    public Transform promotionParent;

    void ShowPromotionUI(Vector2Int from, Vector2Int to, MoveFlag promotionFlag)
    {
        isPromoting = true;

        // Ulo�en� reference na figurku p�ed pohybem
        GameObject movingPiece = pieceObjects.ContainsKey(from) ? pieceObjects[from] : null;

        GameObject panel = Instantiate(promotionPanel, promotionParent);
        panel.transform.position = squares[to.x, to.y].transform.position;

        PromotionUI ui = panel.GetComponent<PromotionUI>();
        ui.Setup(board.IsWhiteTurn, (pieceType) =>
        {
            // 1. Vytvo� tah
            Move move = new Move(
                from.x + from.y * 8,
                to.x + to.y * 8,
                GetPromotionFlag(pieceType)
            );

            // 2. Proveden� tahu (zm�n� board.IsWhiteTurn)
            board.MakeMove(move);

            // 3. Odstran�n� p�ce
            if (movingPiece != null)
            {
                Destroy(movingPiece);
                pieceObjects.Remove(from);
            }

            // 4. Odstran�n� sebran� figurky (pokud existuje)
            if (pieceObjects.ContainsKey(to))
            {
                Destroy(pieceObjects[to]);
                pieceObjects.Remove(to);
            }

            // 5. Vytvo�en� nov� figurky
            PieceType promotedType = move.Flag switch
            {
                MoveFlag.PromotionToQueen => PieceType.Queen,
                MoveFlag.PromotionToRook => PieceType.Rook,
                MoveFlag.PromotionToBishop => PieceType.Bishop,
                MoveFlag.PromotionToKnight => PieceType.Knight,
                _ => PieceType.Queen
            };

            AddPiece(to.x, to.y, promotedType, !board.IsWhiteTurn);

            // 6. Ukon�en�
            Destroy(panel);
            ResetAllHighlights();
            isPromoting = false;
        });
    }
    void CompletePromotionMove(Move move, bool isWhite)
    {
        board.MakeMove(move);

        int file = move.ToSquare % 8;
        int rank = move.ToSquare / 8;
        Vector2Int pos = new Vector2Int(file, rank);

        // Odstran�n� p�ce (pokud je�t� existuje)
        if (pieceObjects.ContainsKey(pos))
        {
            RemovePiece(pos);
        }

        // Vytvo�en� nov� figurky
        PieceType promotedType = move.Flag switch
        {
            MoveFlag.PromotionToQueen => PieceType.Queen,
            MoveFlag.PromotionToRook => PieceType.Rook,
            MoveFlag.PromotionToBishop => PieceType.Bishop,
            MoveFlag.PromotionToKnight => PieceType.Knight,
            _ => PieceType.Queen
        };

        AddPiece(file, rank, promotedType, isWhite);
        ResetAllHighlights();
    }

    MoveFlag GetPromotionFlag(PieceType pieceType)
    {
        return pieceType switch
        {
            PieceType.Queen => MoveFlag.PromotionToQueen,
            PieceType.Rook => MoveFlag.PromotionToRook,
            PieceType.Bishop => MoveFlag.PromotionToBishop,
            PieceType.Knight => MoveFlag.PromotionToKnight,
            _ => MoveFlag.PromotionToQueen // default
        };
    }
}
