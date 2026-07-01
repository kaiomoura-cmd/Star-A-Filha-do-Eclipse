using UnityEngine;
using System.Collections.Generic;

public class LockedGate : MonoBehaviour
{
    [Header("Inimigos que precisam ser derrotados")]
    public List<GameObject> enemiesToDefeat;

    [Header("Abertura")]
    public float openSpeed = 2f;
    public float openHeight = 5f; // Quanto o portão sobe

    private bool isOpen = false;
    private Vector3 closedPosition;
    private Vector3 openPosition;

    void Start()
    {
        closedPosition = transform.position;
        openPosition = closedPosition + Vector3.up * openHeight;
    }

    void Update()
    {
        if (isOpen) return;

        if (AllEnemiesDefeated())
        {
            isOpen = true;
        }
    }

    void FixedUpdate()
    {
        if (!isOpen) return;

        // Mover portão para cima suavemente
        transform.position = Vector3.MoveTowards(
            transform.position,
            openPosition,
            openSpeed * Time.fixedDeltaTime
        );

        // Destruir quando chegar no topo
        if (Vector3.Distance(transform.position, openPosition) < 0.05f)
        {
            gameObject.SetActive(false);
        }
    }

    bool AllEnemiesDefeated()
    {
        foreach (GameObject enemy in enemiesToDefeat)
        {
            if (enemy == null) continue; // Já foi destruído = derrotado

            Enemy e = enemy.GetComponent<Enemy>();
            FlyingEnemy fe = enemy.GetComponent<FlyingEnemy>();
            Brokk b = enemy.GetComponent<Brokk>();

            // Inimigo terrestre: vivo se não morto
            if (e != null && !e.IsDead) return false;

            // Inimigo voador: vivo se não morto E não desabilitado
            if (fe != null && !fe.IsDead && !fe.IsDisabled) return false;

            // Brokk: vivo se não morto
            if (b != null && !b.IsDead) return false;
        }

        return true;
    }
}
