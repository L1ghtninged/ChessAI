using UnityEngine;
using UnityEngine.UI;

public class PromotionUI : MonoBehaviour
{
    [SerializeField] Button queenButton;
    [SerializeField] Button rookButton;
    [SerializeField] Button bishopButton;
    [SerializeField] Button knightButton;

    public void Setup(bool isWhite, System.Action<PieceType> onSelect)
    {
        // M��ete zde nastavit spr�vn� sprite pro figurky
        queenButton.onClick.AddListener(() => onSelect(PieceType.Queen));
        rookButton.onClick.AddListener(() => onSelect(PieceType.Rook));
        bishopButton.onClick.AddListener(() => onSelect(PieceType.Bishop));
        knightButton.onClick.AddListener(() => onSelect(PieceType.Knight));
    }
}
